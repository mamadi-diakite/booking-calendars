namespace BookingCalendars.Application.Auth;

public enum ChangePasswordStatus
{
    Changed = 0,
    UserNotFound = 1,
    Invalid = 2,
}

/// <param name="Errors">Messages by request field, in camelCase, when the status is <see cref="ChangePasswordStatus.Invalid"/>.</param>
public sealed record ChangePasswordResult(ChangePasswordStatus Status, IReadOnlyDictionary<string, string[]> Errors)
{
    public static ChangePasswordResult Changed { get; } = new(ChangePasswordStatus.Changed, new Dictionary<string, string[]>());

    public static ChangePasswordResult UserNotFound { get; } = new(ChangePasswordStatus.UserNotFound, new Dictionary<string, string[]>());

    public static ChangePasswordResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(ChangePasswordStatus.Invalid, errors);
}

/// <param name="Response">The new session, or <c>null</c> when <paramref name="Errors"/> is not empty.</param>
public sealed record RegisterResult(LoginResponse? Response, IReadOnlyDictionary<string, string[]> Errors)
{
    public static RegisterResult Registered(LoginResponse response) => new(response, new Dictionary<string, string[]>());

    public static RegisterResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);
}

/// <param name="User">The updated user, or <c>null</c> when the user is unknown or <paramref name="Errors"/> is not empty.</param>
public sealed record ChangeTimeZoneResult(CurrentUserResponse? User, IReadOnlyDictionary<string, string[]> Errors)
{
    public static ChangeTimeZoneResult UserNotFound { get; } = new(null, new Dictionary<string, string[]>());

    public static ChangeTimeZoneResult Changed(CurrentUserResponse user) => new(user, new Dictionary<string, string[]>());

    public static ChangeTimeZoneResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);
}
