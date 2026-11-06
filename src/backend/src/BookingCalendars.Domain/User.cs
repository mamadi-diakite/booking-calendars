namespace BookingCalendars.Domain;

/// <summary>An account. Any user can own calendars and book slots on other calendars.</summary>
public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;
    public const int PasswordHashMaxLength = 500;

    private User()
    {
    }

    public User(string email, string displayName, string passwordHash, string timeZoneId, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Email = Guard.Email(email, nameof(email), EmailMaxLength);
        DisplayName = Guard.Required(displayName, nameof(displayName), DisplayNameMaxLength);
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), PasswordHashMaxLength);
        TimeZoneId = Guard.TimeZone(timeZoneId, nameof(timeZoneId));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>IANA id used to read the weekly hours of the user's calendars.</summary>
    public string TimeZoneId { get; private set; } = TimeZones.Default;

    public DateTime CreatedAtUtc { get; private set; }

    public static bool IsValidEmail([NotNullWhen(true)] string? email) => Guard.IsEmail(email, EmailMaxLength);

    public void ChangePassword(string passwordHash) =>
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), PasswordHashMaxLength);

    public void ChangeTimeZone(string timeZoneId) => TimeZoneId = Guard.TimeZone(timeZoneId, nameof(timeZoneId));
}
