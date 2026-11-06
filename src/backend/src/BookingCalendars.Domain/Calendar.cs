namespace BookingCalendars.Domain;

/// <summary>A bookable meeting type of an owner, shared through its public link.</summary>
public sealed class Calendar
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 2000;
    public const int LocationNoteMaxLength = 200;

    private readonly List<CalendarMeetingLength> _meetingLengths = [];
    private readonly List<WeeklyHoursRange> _weeklyHours = [];

    private Calendar()
    {
    }

    public Calendar(
        Guid ownerId,
        string title,
        string? description,
        string? locationNote,
        string slug,
        IEnumerable<MeetingLengthSettings> meetingLengths,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        OwnerId = Guard.NotEmpty(ownerId, nameof(ownerId));
        UpdateDetails(title, description, locationNote, slug);
        SetMeetingLengths(meetingLengths);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? LocationNote { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    /// <summary>A paused calendar keeps its settings but takes no bookings.</summary>
    public bool IsPaused { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CalendarMeetingLength> MeetingLengths => _meetingLengths;

    public IReadOnlyCollection<WeeklyHoursRange> WeeklyHours => _weeklyHours;

    public string PublicPath => $"/book/{Slug}";

    /// <param name="slug">Unique among all calendars; the caller checks it is free.</param>
    public void UpdateDetails(string title, string? description, string? locationNote, string slug)
    {
        var checkedTitle = Guard.Required(title, nameof(title), TitleMaxLength);
        var checkedDescription = Guard.Optional(description, nameof(description), DescriptionMaxLength);
        var checkedLocationNote = Guard.Optional(locationNote, nameof(locationNote), LocationNoteMaxLength);
        Slug = CalendarSlug.IsValid(slug) ? slug : throw new ArgumentException("Slug is not valid.", nameof(slug));
        Title = checkedTitle;
        Description = checkedDescription;
        LocationNote = checkedLocationNote;
    }

    public void Pause() => IsPaused = true;

    public void Resume() => IsPaused = false;

    /// <summary>Replaces the offered lengths. Lengths kept in the list keep their identity.</summary>
    public void SetMeetingLengths(IEnumerable<MeetingLengthSettings> meetingLengths)
    {
        ArgumentNullException.ThrowIfNull(meetingLengths);
        var wanted = meetingLengths.ToList();
        if (wanted.Count == 0)
        {
            throw new ArgumentException("A calendar offers at least one meeting length.", nameof(meetingLengths));
        }

        if (wanted.DistinctBy(m => m.Minutes).Count() != wanted.Count)
        {
            throw new ArgumentException("Each meeting length is offered once.", nameof(meetingLengths));
        }

        var updated = wanted
            .Select(settings => _meetingLengths.SingleOrDefault(m => m.Minutes == settings.Minutes) is { } existing
                ? Update(existing, settings)
                : new CalendarMeetingLength(Id, settings))
            .OrderBy(m => m.Minutes)
            .ToList();

        _meetingLengths.Clear();
        _meetingLengths.AddRange(updated);
    }

    /// <summary>Replaces the weekly hours. Ranges of the same day may touch but not overlap.</summary>
    public void SetWeeklyHours(IEnumerable<WeeklyHoursSettings> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        var wanted = ranges.ToList();
        var errors = WeeklyHoursRules.Check(wanted);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(ranges));
        }

        _weeklyHours.Clear();
        _weeklyHours.AddRange(wanted
            .OrderBy(r => r.Day)
            .ThenBy(r => r.Start)
            .Select(r => new WeeklyHoursRange(Id, r.Day, r.Start, r.End)));
    }

    private static CalendarMeetingLength Update(CalendarMeetingLength existing, MeetingLengthSettings settings)
    {
        existing.SetBuffers(settings.BufferBeforeMinutes, settings.BufferAfterMinutes);
        return existing;
    }
}
