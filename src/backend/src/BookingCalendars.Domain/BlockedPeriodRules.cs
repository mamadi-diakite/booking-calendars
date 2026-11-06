namespace BookingCalendars.Domain;

/// <param name="CalendarId"><c>null</c> for a period on all calendars of the owner.</param>
public readonly record struct BlockedPeriodSettings(Guid? CalendarId, DateOnly StartDate, DateOnly EndDate);

public static class BlockedPeriodRules
{
    /// <summary>
    /// Two periods overlap when they share a day and one of them covers the other's calendar.
    /// A period on all calendars covers every calendar.
    /// </summary>
    public static bool Overlap(BlockedPeriodSettings candidate, BlockedPeriod existing)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var shareADay = candidate.StartDate <= existing.EndDate && existing.StartDate <= candidate.EndDate;
        var shareACalendar = candidate.CalendarId is not { } candidateCalendar
            || existing.CalendarId is not { } existingCalendar
            || candidateCalendar == existingCalendar;

        return shareADay && shareACalendar;
    }

    /// <returns>The first existing period that overlaps the candidate, or <c>null</c>.</returns>
    public static BlockedPeriod? FindOverlap(BlockedPeriodSettings candidate, IEnumerable<BlockedPeriod> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);

        return existing.OrderBy(p => p.StartDate).FirstOrDefault(p => Overlap(candidate, p));
    }
}
