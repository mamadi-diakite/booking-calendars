namespace BookingCalendars.Application.Calendars;

public enum CalendarResultStatus
{
    Ok,
    UserNotFound,
    NotFound,
    Invalid,
}

/// <param name="Errors">Messages by request field, in camelCase, when <paramref name="Status"/> is Invalid.</param>
public sealed record CalendarResult<T>(CalendarResultStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors)
    where T : class
{
    public static CalendarResult<T> Of(CalendarResultStatus status) => new(status, null, new Dictionary<string, string[]>());

    public static CalendarResult<T> Ok(T value) => new(CalendarResultStatus.Ok, value, new Dictionary<string, string[]>());

    public static CalendarResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(CalendarResultStatus.Invalid, null, errors);
}
