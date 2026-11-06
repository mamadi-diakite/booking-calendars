namespace BookingCalendars.Domain;

public static class TimeZones
{
    public const int IdMaxLength = 64;
    public const string Default = "UTC";

    /// <summary>Accepts IANA ids only, such as <c>Europe/Paris</c>, so a stored id means the same on every server.</summary>
    public static bool IsValid([NotNullWhen(true)] string? id) =>
        !string.IsNullOrWhiteSpace(id)
        && id.Length <= IdMaxLength
        && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
        && zone.HasIanaId;
}
