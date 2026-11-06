namespace BookingCalendars.Application.Tests;

public sealed class CalendarServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryCalendarRepository _calendars = new();
    private readonly CalendarService _service;
    private readonly User _owner = new("mila@example.com", "Mila", "hash", "Europe/Paris", Now.UtcDateTime);
    private readonly User _other = new("owen@example.com", "Owen", "hash", "UTC", Now.UtcDateTime);

    public CalendarServiceTests()
    {
        _service = new CalendarService(_users, _calendars, new FixedTimeProvider(Now));
        _users.Users.AddRange([_owner, _other]);
    }

    [Fact]
    public async Task Create_builds_the_slug_from_the_title_and_returns_the_calendar()
    {
        var result = await _service.CreateAsync(_owner.Id, Request(
            title: " Intro call ", description: " First talk ", locationNote: " Phone ",
            lengths: [new(30, 5, 10), new(15, 0, 5)]));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
        var calendar = Assert.Single(_calendars.Calendars);
        Assert.Equal(_owner.Id, calendar.OwnerId);
        Assert.Equal(Now.UtcDateTime, calendar.CreatedAtUtc);
        Assert.Equal(1, _calendars.SaveCount);
        var response = result.Value!;
        Assert.Equal(calendar.Id, response.Id);
        Assert.Equal("Intro call", response.Title);
        Assert.Equal("First talk", response.Description);
        Assert.Equal("Phone", response.LocationNote);
        Assert.Equal("intro-call", response.Slug);
        Assert.Equal("/book/intro-call", response.PublicPath);
        Assert.False(response.IsPaused);
        Assert.Equal([new MeetingLengthDto(15, 0, 5), new MeetingLengthDto(30, 5, 10)], response.MeetingLengths);
        Assert.Empty(response.WeeklyHours);
        Assert.Equal(Now.UtcDateTime, response.CreatedAtUtc);
    }

    [Fact]
    public async Task Create_numbers_a_slug_already_taken()
    {
        await _service.CreateAsync(_owner.Id, Request(title: "Intro call"));
        await _service.CreateAsync(_other.Id, Request(title: "Intro call!"));

        var third = await _service.CreateAsync(_owner.Id, Request(title: "intro CALL"));

        Assert.Equal("intro-call-3", third.Value?.Slug);
        Assert.Equal(["intro-call", "intro-call-2", "intro-call-3"], _calendars.Calendars.Select(c => c.Slug));
    }

    [Fact]
    public async Task Create_uses_a_chosen_slug_in_lowercase()
    {
        var result = await _service.CreateAsync(_owner.Id, Request(slug: " Mila-Intro "));

        Assert.Equal("mila-intro", result.Value?.Slug);
    }

    [Fact]
    public async Task Create_refuses_a_chosen_slug_already_used()
    {
        await _service.CreateAsync(_other.Id, Request(slug: "intro"));

        var result = await _service.CreateAsync(_owner.Id, Request(slug: "intro"));

        AssertInvalid(result, "slug", "Another calendar already uses this link.");
        Assert.Single(_calendars.Calendars);
    }

    [Theory]
    [InlineData("intro call")]
    [InlineData("intro--call")]
    [InlineData("-intro")]
    public async Task Create_refuses_an_invalid_slug(string slug)
    {
        var result = await _service.CreateAsync(_owner.Id, Request(slug: slug));

        AssertInvalid(result, "slug", "Use lowercase letters, digits and single hyphens, 60 characters at most.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_requires_a_title(string title)
    {
        var result = await _service.CreateAsync(_owner.Id, Request(title: title));

        AssertInvalid(result, "title", "Title is required.");
    }

    [Fact]
    public async Task Create_limits_the_text_lengths()
    {
        var result = await _service.CreateAsync(_owner.Id, Request(
            title: new string('t', Calendar.TitleMaxLength + 1),
            description: new string('d', Calendar.DescriptionMaxLength + 1),
            locationNote: new string('l', Calendar.LocationNoteMaxLength + 1)));

        Assert.Equal(["description", "locationNote", "title"], result.Errors.Keys.Order());
        Assert.Equal(["Use at most 100 characters."], result.Errors["title"]);
        Assert.Empty(_calendars.Calendars);
    }

    [Fact]
    public async Task Create_needs_at_least_one_meeting_length()
    {
        AssertInvalid(await _service.CreateAsync(_owner.Id, Request(lengths: [])),
            "meetingLengths", "Choose at least one meeting length.");
        AssertInvalid(await _service.CreateAsync(_owner.Id, new CalendarRequest("Call", null, null, null, null!)),
            "meetingLengths", "Choose at least one meeting length.");
    }

    [Fact]
    public async Task Create_reports_every_meeting_length_problem()
    {
        var result = await _service.CreateAsync(_owner.Id, Request(lengths: [new(45, 0, 0), new(45, 0, 0), new(30, -5, 0)]));

        Assert.Equal(
            ["Meeting lengths are 15, 30 or 60 minutes.", "Choose each meeting length once.", "Buffers are between 0 and 120 minutes."],
            result.Errors["meetingLengths"]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(121, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 121)]
    public async Task Create_limits_the_buffers(int before, int after)
    {
        var result = await _service.CreateAsync(_owner.Id, Request(lengths: [new(15, before, after)]));

        AssertInvalid(result, "meetingLengths", "Buffers are between 0 and 120 minutes.");
    }

    [Fact]
    public async Task Create_accepts_the_largest_buffers()
    {
        var result = await _service.CreateAsync(_owner.Id, Request(lengths: [new(60, 120, 120)]));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Create_for_an_unknown_user()
    {
        var result = await _service.CreateAsync(Guid.NewGuid(), Request());

        Assert.Equal(CalendarResultStatus.UserNotFound, result.Status);
        Assert.Empty(_calendars.Calendars);
    }

    [Fact]
    public async Task Lists_only_the_owners_calendars()
    {
        await _service.CreateAsync(_owner.Id, Request(title: "Intro call"));
        await _service.CreateAsync(_other.Id, Request(title: "Review"));

        var result = await _service.ListAsync(_owner.Id);

        Assert.Equal(["Intro call"], result.Value!.Select(c => c.Title));
        Assert.Equal(CalendarResultStatus.UserNotFound, (await _service.ListAsync(Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Get_returns_the_owners_calendar_only()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request())).Value!;

        Assert.Equal(created.Id, (await _service.GetAsync(_owner.Id, created.Id)).Value?.Id);
        Assert.Equal(CalendarResultStatus.NotFound, (await _service.GetAsync(_other.Id, created.Id)).Status);
        Assert.Equal(CalendarResultStatus.UserNotFound, (await _service.GetAsync(Guid.NewGuid(), created.Id)).Status);
    }

    [Fact]
    public async Task Update_changes_the_details_and_lengths()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request(lengths: [new(15, 0, 0)]))).Value!;

        var result = await _service.UpdateAsync(_owner.Id, created.Id, Request(
            title: "Discovery call", description: "New", slug: "discovery", lengths: [new(30, 5, 5), new(60, 10, 10)]));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
        Assert.Equal("Discovery call", result.Value?.Title);
        Assert.Equal("New", result.Value?.Description);
        Assert.Equal("discovery", result.Value?.Slug);
        Assert.Equal([30, 60], result.Value!.MeetingLengths.Select(m => m.Minutes));
        Assert.Equal(2, _calendars.SaveCount);
    }

    [Fact]
    public async Task Update_keeps_the_slug_when_none_is_given_or_the_same_one()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request(title: "Intro call"))).Value!;

        Assert.Equal("intro-call", (await _service.UpdateAsync(_owner.Id, created.Id, Request(title: "Renamed"))).Value?.Slug);
        Assert.Equal("intro-call", (await _service.UpdateAsync(
            _owner.Id, created.Id, Request(title: "Renamed", slug: "intro-call"))).Value?.Slug);
    }

    [Fact]
    public async Task Update_refuses_a_slug_of_another_calendar()
    {
        await _service.CreateAsync(_other.Id, Request(slug: "taken"));
        var created = (await _service.CreateAsync(_owner.Id, Request(slug: "mine"))).Value!;

        var result = await _service.UpdateAsync(_owner.Id, created.Id, Request(slug: "taken"));

        AssertInvalid(result, "slug", "Another calendar already uses this link.");
        Assert.Equal("mine", _calendars.Calendars.Single(c => c.Id == created.Id).Slug);
    }

    [Fact]
    public async Task Update_of_a_calendar_of_someone_else_is_not_found()
    {
        var created = (await _service.CreateAsync(_other.Id, Request())).Value!;

        Assert.Equal(CalendarResultStatus.NotFound, (await _service.UpdateAsync(_owner.Id, created.Id, Request())).Status);
        Assert.Equal(CalendarResultStatus.NotFound, (await _service.UpdateAsync(_owner.Id, Guid.NewGuid(), Request())).Status);
        Assert.Equal(CalendarResultStatus.UserNotFound, (await _service.UpdateAsync(Guid.NewGuid(), created.Id, Request())).Status);
    }

    [Fact]
    public async Task Pauses_and_resumes_a_calendar()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request())).Value!;

        Assert.True((await _service.SetPausedAsync(_owner.Id, created.Id, new PauseCalendarRequest(true))).Value?.IsPaused);
        Assert.True(_calendars.Calendars.Single().IsPaused);
        Assert.False((await _service.SetPausedAsync(_owner.Id, created.Id, new PauseCalendarRequest(false))).Value?.IsPaused);
        Assert.Equal(CalendarResultStatus.NotFound,
            (await _service.SetPausedAsync(_other.Id, created.Id, new PauseCalendarRequest(true))).Status);
    }

    [Fact]
    public async Task Sets_weekly_hours_sorted_by_day_and_time()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request())).Value!;

        var result = await _service.SetWeeklyHoursAsync(_owner.Id, created.Id, new WeeklyHoursRequest(
        [
            new(DayOfWeek.Tuesday, "14:00", "17:30"),
            new(DayOfWeek.Monday, "09:00", "12:00"),
            new(DayOfWeek.Tuesday, "09:00", "12:00"),
        ]));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
        Assert.Equal(
            [
                new WeeklyHoursRangeDto(DayOfWeek.Monday, "09:00", "12:00"),
                new WeeklyHoursRangeDto(DayOfWeek.Tuesday, "09:00", "12:00"),
                new WeeklyHoursRangeDto(DayOfWeek.Tuesday, "14:00", "17:30"),
            ],
            result.Value!.WeeklyHours);
        Assert.Equal(3, _calendars.Calendars.Single().WeeklyHours.Count);
    }

    [Fact]
    public async Task Empty_weekly_hours_clear_the_week()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request())).Value!;
        await _service.SetWeeklyHoursAsync(_owner.Id, created.Id, new WeeklyHoursRequest([new(DayOfWeek.Monday, "09:00", "12:00")]));

        var result = await _service.SetWeeklyHoursAsync(_owner.Id, created.Id, new WeeklyHoursRequest(null!));

        Assert.Empty(result.Value!.WeeklyHours);
    }

    [Fact]
    public async Task Refuses_overlapping_empty_and_badly_written_ranges_with_clear_messages()
    {
        var created = (await _service.CreateAsync(_owner.Id, Request())).Value!;

        var result = await _service.SetWeeklyHoursAsync(_owner.Id, created.Id, new WeeklyHoursRequest(
        [
            new(DayOfWeek.Monday, "09:00", "12:00"),
            new(DayOfWeek.Monday, "11:00", "13:00"),
            new(DayOfWeek.Friday, "10:00", "10:00"),
            new(DayOfWeek.Friday, "9h", "10:00"),
            new(DayOfWeek.Friday, "08:00", "25:00"),
        ]));

        Assert.Equal(CalendarResultStatus.Invalid, result.Status);
        Assert.Equal(
            [
                "Times use the HH:mm format, for example 09:30.",
                "Friday 10:00-10:00: a range must end after it starts.",
                "Monday: 09:00-12:00 and 11:00-13:00 overlap.",
            ],
            result.Errors["ranges"]);
        Assert.Empty(_calendars.Calendars.Single().WeeklyHours);
        Assert.Equal(1, _calendars.SaveCount);
    }

    [Fact]
    public async Task Weekly_hours_of_a_calendar_of_someone_else_are_not_found()
    {
        var created = (await _service.CreateAsync(_other.Id, Request())).Value!;

        var result = await _service.SetWeeklyHoursAsync(_owner.Id, created.Id, new WeeklyHoursRequest([]));

        Assert.Equal(CalendarResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Deletes_the_owners_calendar()
    {
        var mine = (await _service.CreateAsync(_owner.Id, Request(title: "Mine"))).Value!;
        var theirs = (await _service.CreateAsync(_other.Id, Request(title: "Theirs"))).Value!;

        Assert.Equal(CalendarResultStatus.NotFound, await _service.DeleteAsync(_owner.Id, theirs.Id));
        Assert.Equal(CalendarResultStatus.UserNotFound, await _service.DeleteAsync(Guid.NewGuid(), mine.Id));
        Assert.Equal(CalendarResultStatus.Ok, await _service.DeleteAsync(_owner.Id, mine.Id));

        Assert.Equal(["Theirs"], _calendars.Calendars.Select(c => c.Title));
    }

    [Fact]
    public async Task Rejects_missing_requests()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(_owner.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_owner.Id, Guid.NewGuid(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SetPausedAsync(_owner.Id, Guid.NewGuid(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SetWeeklyHoursAsync(_owner.Id, Guid.NewGuid(), null!));
    }

    private void AssertInvalid(CalendarResult<CalendarResponse> result, string field, string message)
    {
        Assert.Equal(CalendarResultStatus.Invalid, result.Status);
        Assert.Null(result.Value);
        Assert.Equal([message], result.Errors[field]);
    }

    private static CalendarRequest Request(
        string title = "Intro call",
        string? description = null,
        string? locationNote = null,
        string? slug = null,
        MeetingLengthDto[]? lengths = null) =>
        new(title, description, locationNote, slug, lengths ?? [new(30, 0, 0)]);
}
