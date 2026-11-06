namespace BookingCalendars.Domain;

public sealed class CalendarMeetingLength
{
    private CalendarMeetingLength()
    {
    }

    internal CalendarMeetingLength(Guid calendarId, MeetingLengthSettings settings)
    {
        Id = Guid.CreateVersion7();
        CalendarId = calendarId;
        Minutes = MeetingLengths.IsAllowed(settings.Minutes)
            ? settings.Minutes
            : throw new ArgumentOutOfRangeException(
                "meetingLengths", settings.Minutes, "A meeting lasts 15, 30 or 60 minutes.");
        SetBuffers(settings.BufferBeforeMinutes, settings.BufferAfterMinutes);
    }

    public Guid Id { get; private set; }

    public Guid CalendarId { get; private set; }

    public int Minutes { get; private set; }

    public int BufferBeforeMinutes { get; private set; }

    public int BufferAfterMinutes { get; private set; }

    internal void SetBuffers(int bufferBeforeMinutes, int bufferAfterMinutes)
    {
        BufferBeforeMinutes = Guard.InRange(bufferBeforeMinutes, "meetingLengths", 0, MeetingLengths.MaxBufferMinutes);
        BufferAfterMinutes = Guard.InRange(bufferAfterMinutes, "meetingLengths", 0, MeetingLengths.MaxBufferMinutes);
    }
}
