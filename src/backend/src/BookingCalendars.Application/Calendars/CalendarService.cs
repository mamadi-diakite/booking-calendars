namespace BookingCalendars.Application.Calendars;

public sealed class CalendarService(IUserRepository users, ICalendarRepository calendars, TimeProvider timeProvider)
{
    private const string TimeFormat = "HH:mm";

    public async Task<CalendarResult<IReadOnlyList<CalendarResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (await users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return CalendarResult<IReadOnlyList<CalendarResponse>>.Of(CalendarResultStatus.UserNotFound);
        }

        var list = await calendars.ListAsync(userId, cancellationToken);

        return CalendarResult<IReadOnlyList<CalendarResponse>>.Ok([.. list.Select(ToResponse)]);
    }

    public async Task<CalendarResult<CalendarResponse>> GetAsync(
        Guid userId,
        Guid calendarId,
        CancellationToken cancellationToken = default)
    {
        var (status, calendar) = await FindCalendarAsync(userId, calendarId, cancellationToken);

        return calendar is null
            ? CalendarResult<CalendarResponse>.Of(status)
            : CalendarResult<CalendarResponse>.Ok(ToResponse(calendar));
    }

    /// <summary>Creates a calendar with no weekly hours yet. The slug is built from the title when none is given.</summary>
    public async Task<CalendarResult<CalendarResponse>> CreateAsync(
        Guid userId,
        CalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return CalendarResult<CalendarResponse>.Of(CalendarResultStatus.UserNotFound);
        }

        var errors = await CheckAsync(request, null, cancellationToken);
        if (errors.Count > 0)
        {
            return CalendarResult<CalendarResponse>.Invalid(errors);
        }

        var slug = RequestedSlug(request) ?? await FreeSlugAsync(request.Title, cancellationToken);
        var calendar = new Calendar(
            userId,
            request.Title,
            request.Description,
            request.LocationNote,
            slug,
            ToSettings(request.MeetingLengths),
            timeProvider.GetUtcNow().UtcDateTime);
        calendars.Add(calendar);
        await calendars.SaveChangesAsync(cancellationToken);

        return CalendarResult<CalendarResponse>.Ok(ToResponse(calendar));
    }

    /// <summary>Updates the details and lengths. An empty slug keeps the current one.</summary>
    public async Task<CalendarResult<CalendarResponse>> UpdateAsync(
        Guid userId,
        Guid calendarId,
        CalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (status, calendar) = await FindCalendarAsync(userId, calendarId, cancellationToken);
        if (calendar is null)
        {
            return CalendarResult<CalendarResponse>.Of(status);
        }

        var errors = await CheckAsync(request, calendar.Id, cancellationToken);
        if (errors.Count > 0)
        {
            return CalendarResult<CalendarResponse>.Invalid(errors);
        }

        calendar.UpdateDetails(request.Title, request.Description, request.LocationNote, RequestedSlug(request) ?? calendar.Slug);
        calendar.SetMeetingLengths(ToSettings(request.MeetingLengths));
        await calendars.SaveChangesAsync(cancellationToken);

        return CalendarResult<CalendarResponse>.Ok(ToResponse(calendar));
    }

    public async Task<CalendarResult<CalendarResponse>> SetPausedAsync(
        Guid userId,
        Guid calendarId,
        PauseCalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (status, calendar) = await FindCalendarAsync(userId, calendarId, cancellationToken);
        if (calendar is null)
        {
            return CalendarResult<CalendarResponse>.Of(status);
        }

        if (request.IsPaused)
        {
            calendar.Pause();
        }
        else
        {
            calendar.Resume();
        }

        await calendars.SaveChangesAsync(cancellationToken);

        return CalendarResult<CalendarResponse>.Ok(ToResponse(calendar));
    }

    /// <summary>Replaces the weekly hours. Times are read in the owner's time zone.</summary>
    public async Task<CalendarResult<CalendarResponse>> SetWeeklyHoursAsync(
        Guid userId,
        Guid calendarId,
        WeeklyHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (status, calendar) = await FindCalendarAsync(userId, calendarId, cancellationToken);
        if (calendar is null)
        {
            return CalendarResult<CalendarResponse>.Of(status);
        }

        var ranges = new List<WeeklyHoursSettings>();
        var messages = new List<string>();
        foreach (var range in request.Ranges ?? [])
        {
            if (TryParseTime(range.Start, out var start) && TryParseTime(range.End, out var end))
            {
                ranges.Add(new WeeklyHoursSettings(range.Day, start, end));
            }
            else
            {
                messages.Add("Times use the HH:mm format, for example 09:30.");
            }
        }

        messages.AddRange(WeeklyHoursRules.Check(ranges));
        if (messages.Count > 0)
        {
            return CalendarResult<CalendarResponse>.Invalid(
                new Dictionary<string, string[]> { ["ranges"] = [.. messages.Distinct()] });
        }

        calendar.SetWeeklyHours(ranges);
        await calendars.SaveChangesAsync(cancellationToken);

        return CalendarResult<CalendarResponse>.Ok(ToResponse(calendar));
    }

    /// <summary>Deletes the calendar with its weekly hours and its own blocked periods.</summary>
    public async Task<CalendarResultStatus> DeleteAsync(Guid userId, Guid calendarId, CancellationToken cancellationToken = default)
    {
        var (status, calendar) = await FindCalendarAsync(userId, calendarId, cancellationToken);
        if (calendar is null)
        {
            return status;
        }

        calendars.Remove(calendar);
        await calendars.SaveChangesAsync(cancellationToken);

        return CalendarResultStatus.Ok;
    }

    private async Task<(CalendarResultStatus Status, Calendar? Calendar)> FindCalendarAsync(
        Guid userId,
        Guid calendarId,
        CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return (CalendarResultStatus.UserNotFound, null);
        }

        var calendar = await calendars.FindAsync(userId, calendarId, cancellationToken);

        return calendar is null ? (CalendarResultStatus.NotFound, null) : (CalendarResultStatus.Ok, calendar);
    }

    private async Task<Dictionary<string, string[]>> CheckAsync(
        CalendarRequest request,
        Guid? calendarId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Trim().Length > Calendar.TitleMaxLength)
        {
            errors["title"] = [$"Use at most {Calendar.TitleMaxLength} characters."];
        }

        if (request.Description?.Trim().Length > Calendar.DescriptionMaxLength)
        {
            errors["description"] = [$"Use at most {Calendar.DescriptionMaxLength} characters."];
        }

        if (request.LocationNote?.Trim().Length > Calendar.LocationNoteMaxLength)
        {
            errors["locationNote"] = [$"Use at most {Calendar.LocationNoteMaxLength} characters."];
        }

        if (RequestedSlug(request) is { } slug)
        {
            if (!CalendarSlug.IsValid(slug))
            {
                errors["slug"] = [$"Use lowercase letters, digits and single hyphens, {CalendarSlug.MaxLength} characters at most."];
            }
            else if (await calendars.SlugExistsAsync(slug, calendarId, cancellationToken))
            {
                errors["slug"] = ["Another calendar already uses this link."];
            }
        }

        var lengthErrors = CheckMeetingLengths(request.MeetingLengths);
        if (lengthErrors.Count > 0)
        {
            errors["meetingLengths"] = [.. lengthErrors];
        }

        return errors;
    }

    private static List<string> CheckMeetingLengths(IReadOnlyList<MeetingLengthDto>? lengths)
    {
        if (lengths is null || lengths.Count == 0)
        {
            return ["Choose at least one meeting length."];
        }

        var errors = new List<string>();
        if (!lengths.All(l => MeetingLengths.IsAllowed(l.Minutes)))
        {
            errors.Add("Meeting lengths are 15, 30 or 60 minutes.");
        }

        if (lengths.DistinctBy(l => l.Minutes).Count() != lengths.Count)
        {
            errors.Add("Choose each meeting length once.");
        }

        if (!lengths.All(l => IsBuffer(l.BufferBeforeMinutes) && IsBuffer(l.BufferAfterMinutes)))
        {
            errors.Add($"Buffers are between 0 and {MeetingLengths.MaxBufferMinutes} minutes.");
        }

        return errors;
    }

    private static bool IsBuffer(int minutes) => minutes is >= 0 and <= MeetingLengths.MaxBufferMinutes;

    private static string? RequestedSlug(CalendarRequest request) =>
        string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant();

    private async Task<string> FreeSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = CalendarSlug.FromTitle(title);
        var slug = baseSlug;
        for (var number = 2; await calendars.SlugExistsAsync(slug, null, cancellationToken); number++)
        {
            slug = CalendarSlug.WithNumber(baseSlug, number);
        }

        return slug;
    }

    private static IEnumerable<MeetingLengthSettings> ToSettings(IEnumerable<MeetingLengthDto> lengths) =>
        lengths.Select(l => new MeetingLengthSettings(l.Minutes, l.BufferBeforeMinutes, l.BufferAfterMinutes));

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    private static CalendarResponse ToResponse(Calendar calendar) => new(
        calendar.Id,
        calendar.Title,
        calendar.Description,
        calendar.LocationNote,
        calendar.Slug,
        calendar.PublicPath,
        calendar.IsPaused,
        [.. calendar.MeetingLengths
            .OrderBy(m => m.Minutes)
            .Select(m => new MeetingLengthDto(m.Minutes, m.BufferBeforeMinutes, m.BufferAfterMinutes))],
        [.. calendar.WeeklyHours
            .OrderBy(r => r.Day)
            .ThenBy(r => r.Start)
            .Select(r => new WeeklyHoursRangeDto(
                r.Day,
                r.Start.ToString(TimeFormat, CultureInfo.InvariantCulture),
                r.End.ToString(TimeFormat, CultureInfo.InvariantCulture)))],
        calendar.CreatedAtUtc);
}
