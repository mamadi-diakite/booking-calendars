namespace BookingCalendars.Application.Calendars;

public sealed class BlockedPeriodService(
    IUserRepository users,
    ICalendarRepository calendars,
    IBlockedPeriodRepository periods,
    TimeProvider timeProvider)
{
    public async Task<CalendarResult<IReadOnlyList<BlockedPeriodResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (await users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return CalendarResult<IReadOnlyList<BlockedPeriodResponse>>.Of(CalendarResultStatus.UserNotFound);
        }

        var list = await periods.ListAsync(userId, cancellationToken);

        return CalendarResult<IReadOnlyList<BlockedPeriodResponse>>.Ok([.. list.Select(ToResponse)]);
    }

    /// <summary>Blocks days on one calendar, or on all calendars when no calendar is given.</summary>
    public async Task<CalendarResult<BlockedPeriodResponse>> CreateAsync(
        Guid userId,
        BlockedPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return CalendarResult<BlockedPeriodResponse>.Of(CalendarResultStatus.UserNotFound);
        }

        var errors = new Dictionary<string, string[]>();
        if (request.CalendarId is { } calendarId && !await calendars.ExistsAsync(userId, calendarId, cancellationToken))
        {
            errors["calendarId"] = ["Choose one of your calendars."];
        }

        if (request.EndDate < request.StartDate)
        {
            errors["endDate"] = ["The period cannot end before it starts."];
        }

        if (request.Label?.Trim().Length > BlockedPeriod.LabelMaxLength)
        {
            errors["label"] = [$"Use at most {BlockedPeriod.LabelMaxLength} characters."];
        }

        if (errors.Count == 0)
        {
            var candidate = new BlockedPeriodSettings(request.CalendarId, request.StartDate, request.EndDate);
            if (BlockedPeriodRules.FindOverlap(candidate, await periods.ListAsync(userId, cancellationToken)) is { } overlap)
            {
                errors["startDate"] = [$"This period overlaps the blocked period {Describe(overlap)}."];
            }
        }

        if (errors.Count > 0)
        {
            return CalendarResult<BlockedPeriodResponse>.Invalid(errors);
        }

        var period = new BlockedPeriod(
            userId,
            request.CalendarId,
            request.StartDate,
            request.EndDate,
            request.Label,
            timeProvider.GetUtcNow().UtcDateTime);
        periods.Add(period);
        await periods.SaveChangesAsync(cancellationToken);

        return CalendarResult<BlockedPeriodResponse>.Ok(ToResponse(period));
    }

    public async Task<CalendarResultStatus> DeleteAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default)
    {
        if (await users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return CalendarResultStatus.UserNotFound;
        }

        var period = await periods.FindAsync(userId, periodId, cancellationToken);
        if (period is null)
        {
            return CalendarResultStatus.NotFound;
        }

        periods.Remove(period);
        await periods.SaveChangesAsync(cancellationToken);

        return CalendarResultStatus.Ok;
    }

    private static string Describe(BlockedPeriod period)
    {
        var dates = period.StartDate == period.EndDate
            ? $"on {period.StartDate:yyyy-MM-dd}"
            : $"from {period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}";

        return period.Label is null ? dates : $"\"{period.Label}\" {dates}";
    }

    private static BlockedPeriodResponse ToResponse(BlockedPeriod period) =>
        new(period.Id, period.CalendarId, period.StartDate, period.EndDate, period.Label);
}
