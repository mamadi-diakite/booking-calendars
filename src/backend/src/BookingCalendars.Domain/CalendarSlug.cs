namespace BookingCalendars.Domain;

/// <summary>The last part of a public link: <c>/book/{slug}</c>.</summary>
public static class CalendarSlug
{
    public const int MaxLength = 60;

    /// <summary>Lowercase letters and digits, in words joined by single hyphens.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
        && !value.StartsWith('-')
        && !value.EndsWith('-')
        && !value.Contains("--", StringComparison.Ordinal);
}
