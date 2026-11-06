namespace BookingCalendars.Domain;

/// <summary>The last part of a public link: <c>/book/{slug}</c>.</summary>
public static class CalendarSlug
{
    public const int MaxLength = 60;
    public const string Fallback = "calendar";

    /// <summary>Lowercase letters and digits, in words joined by single hyphens.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
        && !value.StartsWith('-')
        && !value.EndsWith('-')
        && !value.Contains("--", StringComparison.Ordinal);

    /// <summary>Builds a valid slug from a title: accents removed, other signs turned into hyphens.</summary>
    public static string FromTitle(string? title)
    {
        var builder = new StringBuilder();
        foreach (var c in (title ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(c);
            if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                builder.Append(lower);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = Cut(builder.ToString(), MaxLength);

        return slug.Length == 0 ? Fallback : slug;
    }

    /// <summary>Adds a number to a taken slug, for example <c>intro-call-2</c>, within the length limit.</summary>
    public static string WithNumber(string slug, int number)
    {
        var suffix = $"-{number}";

        return Cut(slug, MaxLength - suffix.Length) + suffix;
    }

    private static string Cut(string value, int maxLength) =>
        (value.Length > maxLength ? value[..maxLength] : value).Trim('-');
}
