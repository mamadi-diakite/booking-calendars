namespace BookingCalendars.Application.Tests;

public sealed class BlockedPeriodServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Day = new(2026, 12, 24);

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryCalendarRepository _calendars = new();
    private readonly InMemoryBlockedPeriodRepository _periods = new();
    private readonly BlockedPeriodService _service;
    private readonly User _owner = new("mila@example.com", "Mila", "hash", "Europe/Paris", Now.UtcDateTime);
    private readonly User _other = new("owen@example.com", "Owen", "hash", "UTC", Now.UtcDateTime);
    private readonly Calendar _intro;
    private readonly Calendar _coaching;
    private readonly Calendar _othersCalendar;

    public BlockedPeriodServiceTests()
    {
        _service = new BlockedPeriodService(_users, _calendars, _periods, new FixedTimeProvider(Now));
        _users.Users.AddRange([_owner, _other]);
        _intro = NewCalendar(_owner, "intro");
        _coaching = NewCalendar(_owner, "coaching");
        _othersCalendar = NewCalendar(_other, "review");
        _calendars.Calendars.AddRange([_intro, _coaching, _othersCalendar]);
    }

    [Fact]
    public async Task Blocks_days_on_one_calendar()
    {
        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_intro.Id, Day, Day.AddDays(2), " Holidays "));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
        var period = Assert.Single(_periods.Periods);
        Assert.Equal(new BlockedPeriodResponse(period.Id, _intro.Id, Day, Day.AddDays(2), "Holidays"), result.Value);
        Assert.Equal(_owner.Id, period.OwnerId);
        Assert.Equal(Now.UtcDateTime, period.CreatedAtUtc);
        Assert.Equal(1, _periods.SaveCount);
    }

    [Fact]
    public async Task Blocks_one_day_on_all_calendars()
    {
        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(null, Day, Day, null));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
        Assert.Null(result.Value?.CalendarId);
        Assert.Null(result.Value?.Label);
    }

    [Fact]
    public async Task Periods_on_two_calendars_may_share_days()
    {
        await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_intro.Id, Day, Day.AddDays(2), null));

        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_coaching.Id, Day, Day.AddDays(2), null));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
        Assert.Equal(2, _periods.Periods.Count);
    }

    [Fact]
    public async Task Refuses_a_period_overlapping_another_on_the_same_calendar()
    {
        await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_intro.Id, Day, Day.AddDays(2), "Holidays"));

        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_intro.Id, Day.AddDays(2), Day.AddDays(4), null));

        AssertInvalid(result, "startDate", "This period overlaps the blocked period \"Holidays\" from 2026-12-24 to 2026-12-26.");
    }

    [Fact]
    public async Task Refuses_a_period_on_all_calendars_overlapping_a_period_on_one_calendar()
    {
        await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_coaching.Id, Day, Day, null));

        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(null, Day.AddDays(-1), Day.AddDays(1), null));

        AssertInvalid(result, "startDate", "This period overlaps the blocked period on 2026-12-24.");
    }

    [Fact]
    public async Task Periods_of_other_owners_do_not_count()
    {
        await _service.CreateAsync(_other.Id, new BlockedPeriodRequest(null, Day, Day, null));

        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(null, Day, Day, null));

        Assert.Equal(CalendarResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Refuses_a_period_ending_before_it_starts()
    {
        var result = await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(null, Day, Day.AddDays(-1), null));

        AssertInvalid(result, "endDate", "The period cannot end before it starts.");
    }

    [Fact]
    public async Task Refuses_a_calendar_of_someone_else_or_unknown()
    {
        AssertInvalid(await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_othersCalendar.Id, Day, Day, null)),
            "calendarId", "Choose one of your calendars.");
        AssertInvalid(await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(Guid.NewGuid(), Day, Day, null)),
            "calendarId", "Choose one of your calendars.");
    }

    [Fact]
    public async Task Limits_the_label_length()
    {
        var result = await _service.CreateAsync(
            _owner.Id, new BlockedPeriodRequest(null, Day, Day, new string('x', BlockedPeriod.LabelMaxLength + 1)));

        AssertInvalid(result, "label", "Use at most 100 characters.");
    }

    [Fact]
    public async Task Create_for_an_unknown_user()
    {
        var result = await _service.CreateAsync(Guid.NewGuid(), new BlockedPeriodRequest(null, Day, Day, null));

        Assert.Equal(CalendarResultStatus.UserNotFound, result.Status);
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(_owner.Id, null!));
    }

    [Fact]
    public async Task Lists_the_owners_periods_by_start_date()
    {
        await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(null, Day.AddDays(10), Day.AddDays(10), "Later"));
        await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(_intro.Id, Day, Day, "Sooner"));
        await _service.CreateAsync(_other.Id, new BlockedPeriodRequest(null, Day, Day, "Not mine"));

        var result = await _service.ListAsync(_owner.Id);

        Assert.Equal(["Sooner", "Later"], result.Value!.Select(p => p.Label));
        Assert.Equal(CalendarResultStatus.UserNotFound, (await _service.ListAsync(Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Deletes_the_owners_period()
    {
        var mine = (await _service.CreateAsync(_owner.Id, new BlockedPeriodRequest(null, Day, Day, "Mine"))).Value!;
        var theirs = (await _service.CreateAsync(_other.Id, new BlockedPeriodRequest(null, Day, Day, "Theirs"))).Value!;

        Assert.Equal(CalendarResultStatus.NotFound, await _service.DeleteAsync(_owner.Id, theirs.Id));
        Assert.Equal(CalendarResultStatus.UserNotFound, await _service.DeleteAsync(Guid.NewGuid(), mine.Id));
        Assert.Equal(CalendarResultStatus.Ok, await _service.DeleteAsync(_owner.Id, mine.Id));

        Assert.Equal(["Theirs"], _periods.Periods.Select(p => p.Label));
    }

    private void AssertInvalid(CalendarResult<BlockedPeriodResponse> result, string field, string message)
    {
        Assert.Equal(CalendarResultStatus.Invalid, result.Status);
        Assert.Null(result.Value);
        Assert.Equal([message], result.Errors[field]);
    }

    private static Calendar NewCalendar(User owner, string slug) =>
        new(owner.Id, slug, null, null, slug, [new MeetingLengthSettings(30, 0, 0)], Now.UtcDateTime);
}
