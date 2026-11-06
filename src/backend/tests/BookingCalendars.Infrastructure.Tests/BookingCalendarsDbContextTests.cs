namespace BookingCalendars.Infrastructure.Tests;

public sealed class BookingCalendarsDbContextTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _owner = new("clara.weber@example.com", "Clara Weber", "hash", "Europe/Berlin", Now);

    public BookingCalendarsDbContextTests()
    {
        using var db = _database.CreateContext();
        db.Users.Add(_owner);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_a_user_with_its_time_zone()
    {
        await using var db = _database.CreateContext();
        var user = await db.Users.SingleAsync();

        Assert.Equal("clara.weber@example.com", user.Email);
        Assert.Equal("Europe/Berlin", user.TimeZoneId);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task Email_is_unique()
    {
        await using var db = _database.CreateContext();
        db.Users.Add(new User("Clara.Weber@Example.com", "Clara again", "hash", "UTC", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Saves_and_reads_a_calendar_with_its_meeting_lengths()
    {
        await AddCalendarAsync("intro-call", [new(15, 5, 5), new(60, 15, 10)]);

        await using var db = _database.CreateContext();
        var calendar = await db.Calendars.Include(c => c.MeetingLengths).SingleAsync();

        Assert.Equal("Intro call", calendar.Title);
        Assert.Equal(_owner.Id, calendar.OwnerId);
        Assert.Equal([15, 60], calendar.MeetingLengths.OrderBy(m => m.Minutes).Select(m => m.Minutes));
        var hour = calendar.MeetingLengths.Single(m => m.Minutes == 60);
        Assert.Equal((15, 10), (hour.BufferBeforeMinutes, hour.BufferAfterMinutes));
        Assert.Equal(DateTimeKind.Utc, calendar.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task Changed_meeting_lengths_are_saved()
    {
        var calendar = await AddCalendarAsync("intro-call", [new(15, 0, 0), new(30, 0, 0)]);

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Calendars.Include(c => c.MeetingLengths).SingleAsync();
            loaded.SetMeetingLengths([new(30, 5, 5), new(60, 0, 10)]);
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        var lengths = await check.CalendarMeetingLengths.OrderBy(m => m.Minutes).ToListAsync();
        Assert.Equal([30, 60], lengths.Select(m => m.Minutes));
        Assert.Equal(calendar.MeetingLengths.Single(m => m.Minutes == 30).Id, lengths[0].Id);
        Assert.Equal(5, lengths[0].BufferBeforeMinutes);
    }

    [Fact]
    public async Task Replaced_weekly_hours_are_saved()
    {
        await AddCalendarAsync("intro-call", [new(30, 0, 0)]);
        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Calendars.Include(c => c.WeeklyHours).SingleAsync();
            loaded.SetWeeklyHours([new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0))]);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Calendars.Include(c => c.WeeklyHours).SingleAsync();
            loaded.SetWeeklyHours(
            [
                new(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
                new(DayOfWeek.Tuesday, new TimeOnly(13, 30), new TimeOnly(17, 0)),
            ]);
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        var ranges = await check.WeeklyHours.OrderBy(r => r.Start).ToListAsync();
        Assert.Equal(2, ranges.Count);
        Assert.All(ranges, r => Assert.Equal(DayOfWeek.Tuesday, r.Day));
        Assert.Equal(new TimeOnly(13, 30), ranges[1].Start);
        Assert.Equal(new TimeOnly(17, 0), ranges[1].End);
    }

    [Fact]
    public async Task Slug_is_unique()
    {
        await AddCalendarAsync("intro-call", [new(30, 0, 0)]);

        await Assert.ThrowsAsync<DbUpdateException>(() => AddCalendarAsync("intro-call", [new(30, 0, 0)]));
    }

    [Fact]
    public async Task Saves_blocked_periods_on_one_or_all_calendars()
    {
        var calendar = await AddCalendarAsync("intro-call", [new(30, 0, 0)]);
        await using (var db = _database.CreateContext())
        {
            db.BlockedPeriods.AddRange(
                new BlockedPeriod(_owner.Id, calendar.Id, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), "Holidays", Now),
                new BlockedPeriod(_owner.Id, null, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 1), null, Now));
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        var periods = await check.BlockedPeriods.OrderBy(p => p.StartDate).ToListAsync();
        Assert.Equal(calendar.Id, periods[0].CalendarId);
        Assert.Equal(new DateOnly(2026, 12, 26), periods[0].EndDate);
        Assert.Equal("Holidays", periods[0].Label);
        Assert.Null(periods[1].CalendarId);
    }

    [Fact]
    public async Task Deleting_a_calendar_deletes_its_lengths_and_blocked_periods()
    {
        var calendar = await AddCalendarAsync("intro-call", [new(30, 0, 0)]);
        await using (var db = _database.CreateContext())
        {
            db.BlockedPeriods.Add(new BlockedPeriod(
                _owner.Id, calendar.Id, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 24), null, Now));
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            db.Calendars.Remove(await db.Calendars.SingleAsync());
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        Assert.Equal(0, await check.CalendarMeetingLengths.CountAsync());
        Assert.Equal(0, await check.BlockedPeriods.CountAsync());
    }

    [Fact]
    public async Task An_owner_with_calendars_cannot_be_deleted()
    {
        await AddCalendarAsync("intro-call", [new(30, 0, 0)]);

        await using var db = _database.CreateContext();
        db.Users.Remove(await db.Users.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private async Task<Calendar> AddCalendarAsync(string slug, MeetingLengthSettings[] meetingLengths)
    {
        var calendar = new Calendar(_owner.Id, "Intro call", null, null, slug, meetingLengths, Now);

        await using var db = _database.CreateContext();
        db.Calendars.Add(calendar);
        await db.SaveChangesAsync();

        return calendar;
    }
}
