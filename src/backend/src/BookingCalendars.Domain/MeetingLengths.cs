namespace BookingCalendars.Domain;

public static class MeetingLengths
{
    public const int MaxBufferMinutes = 120;

    public static IReadOnlyList<int> AllowedMinutes { get; } = [15, 30, 60];

    public static bool IsAllowed(int minutes) => AllowedMinutes.Contains(minutes);
}

/// <summary>A meeting length offered by a calendar, with the free time kept before and after it.</summary>
public readonly record struct MeetingLengthSettings(int Minutes, int BufferBeforeMinutes, int BufferAfterMinutes);
