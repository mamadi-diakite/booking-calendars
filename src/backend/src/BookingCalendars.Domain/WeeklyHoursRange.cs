namespace BookingCalendars.Domain;

/// <summary>A time range on a weekday, read in the owner's time zone.</summary>
public sealed class WeeklyHoursRange
{
    private WeeklyHoursRange()
    {
    }

    internal WeeklyHoursRange(Guid calendarId, DayOfWeek day, TimeOnly start, TimeOnly end)
    {
        Id = Guid.CreateVersion7();
        CalendarId = calendarId;
        Day = Guard.Defined(day, nameof(day));
        Start = start;
        End = end > start ? end : throw new ArgumentException("A range must end after it starts.", nameof(end));
    }

    public Guid Id { get; private set; }

    public Guid CalendarId { get; private set; }

    public DayOfWeek Day { get; private set; }

    public TimeOnly Start { get; private set; }

    public TimeOnly End { get; private set; }
}
