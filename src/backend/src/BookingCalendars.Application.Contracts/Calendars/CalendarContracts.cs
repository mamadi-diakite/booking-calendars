namespace BookingCalendars.Application.Contracts.Calendars;

/// <param name="Minutes">15, 30 or 60.</param>
/// <param name="BufferBeforeMinutes">Free time kept before the meeting, 0 to 120 minutes.</param>
/// <param name="BufferAfterMinutes">Free time kept after the meeting, 0 to 120 minutes.</param>
public sealed record MeetingLengthDto(int Minutes, int BufferBeforeMinutes, int BufferAfterMinutes);

/// <param name="Day">0 for Sunday to 6 for Saturday.</param>
/// <param name="Start">Local time in the owner's time zone, as HH:mm.</param>
/// <param name="End">Local time in the owner's time zone, as HH:mm, after <paramref name="Start"/>.</param>
public sealed record WeeklyHoursRangeDto(DayOfWeek Day, string Start, string End);

/// <param name="Slug">Last part of the public link. Built from the title when left empty.</param>
public sealed record CalendarRequest(
    string Title,
    string? Description,
    string? LocationNote,
    string? Slug,
    IReadOnlyList<MeetingLengthDto> MeetingLengths);

public sealed record PauseCalendarRequest(bool IsPaused);

public sealed record WeeklyHoursRequest(IReadOnlyList<WeeklyHoursRangeDto> Ranges);

/// <param name="PublicPath">Shareable path of the calendar, such as <c>/book/intro-call</c>.</param>
public sealed record CalendarResponse(
    Guid Id,
    string Title,
    string? Description,
    string? LocationNote,
    string Slug,
    string PublicPath,
    bool IsPaused,
    IReadOnlyList<MeetingLengthDto> MeetingLengths,
    IReadOnlyList<WeeklyHoursRangeDto> WeeklyHours,
    DateTime CreatedAtUtc);

/// <param name="CalendarId"><c>null</c> to block all calendars of the owner.</param>
/// <param name="EndDate">Last blocked day, included.</param>
public sealed record BlockedPeriodRequest(Guid? CalendarId, DateOnly StartDate, DateOnly EndDate, string? Label);

/// <param name="CalendarId"><c>null</c> when the period blocks all calendars of the owner.</param>
public sealed record BlockedPeriodResponse(Guid Id, Guid? CalendarId, DateOnly StartDate, DateOnly EndDate, string? Label);
