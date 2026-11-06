namespace BookingCalendars.Domain;

/// <summary>Days when no booking is possible, on one calendar or on all calendars of the owner.</summary>
public sealed class BlockedPeriod
{
    public const int LabelMaxLength = 100;

    private BlockedPeriod()
    {
    }

    /// <param name="calendarId"><c>null</c> to block all calendars of the owner.</param>
    /// <param name="startDate">First blocked day, in the owner's time zone.</param>
    /// <param name="endDate">Last blocked day, included.</param>
    public BlockedPeriod(
        Guid ownerId,
        Guid? calendarId,
        DateOnly startDate,
        DateOnly endDate,
        string? label,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        OwnerId = Guard.NotEmpty(ownerId, nameof(ownerId));
        CalendarId = calendarId is { } id ? Guard.NotEmpty(id, nameof(calendarId)) : null;
        StartDate = startDate;
        EndDate = endDate >= startDate
            ? endDate
            : throw new ArgumentException("A blocked period cannot end before it starts.", nameof(endDate));
        Label = Guard.Optional(label, nameof(label), LabelMaxLength);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid? CalendarId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public string? Label { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public bool AppliesToAllCalendars => CalendarId is null;
}
