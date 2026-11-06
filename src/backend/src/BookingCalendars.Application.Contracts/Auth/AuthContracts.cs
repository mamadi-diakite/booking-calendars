namespace BookingCalendars.Application.Contracts.Auth;

/// <param name="TimeZone">IANA id, usually the browser's. UTC when left empty.</param>
public sealed record RegisterRequest(
    string DisplayName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? TimeZone = null);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, CurrentUserResponse User);

/// <param name="TimeZone">IANA id used to read the weekly hours of the user's calendars.</param>
public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, string TimeZone);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public sealed record ChangeTimeZoneRequest(string TimeZone);
