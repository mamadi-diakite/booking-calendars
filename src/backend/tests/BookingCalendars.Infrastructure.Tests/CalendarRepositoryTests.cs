namespace BookingCalendars.Infrastructure.Tests;

public sealed class CalendarRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _owner = new("mila@example.com", "Mila", "hash", "Europe/Paris", Now);
    private readonly User _other = new("owen@example.com", "Owen", "hash", "UTC", Now);
    private readonly Calendar _intro;
    private readonly Calendar _coaching;
    private readonly Calendar _review;

    public CalendarRepositoryTests()
    {
        _intro = new Calendar(_owner.Id, "Intro call", null, null, "intro-call", [new(15, 0, 0), new(30, 5, 5)], Now);
        _intro.SetWeeklyHours([new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0))]);
        _coaching = new Calendar(_owner.Id, "Coaching", null, null, "coaching", [new(60, 0, 0)], Now.AddMinutes(1));
        _review = new Calendar(_other.Id, "Review", null, null, "review", [new(30, 0, 0)], Now);

        using var db = _database.CreateContext();
        db.Users.AddRange(_owner, _other);
        db.Calendars.AddRange(_coaching, _intro, _review);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_the_owners_calendars_oldest_first_with_their_rules()
    {
        await using var db = _database.CreateContext();

        var list = await new CalendarRepository(db).ListAsync(_owner.Id, default);

        Assert.Equal(["intro-call", "coaching"], list.Select(c => c.Slug));
        Assert.Equal(2, list[0].MeetingLengths.Count);
        Assert.Single(list[0].WeeklyHours);
    }

    [Fact]
    public async Task Finds_only_the_owners_calendar_with_its_rules()
    {
        await using var db = _database.CreateContext();
        var repository = new CalendarRepository(db);

        var found = await repository.FindAsync(_owner.Id, _intro.Id, default);

        Assert.Equal(2, found?.MeetingLengths.Count);
        Assert.Single(found!.WeeklyHours);
        Assert.Null(await repository.FindAsync(_owner.Id, _review.Id, default));
        Assert.True(await repository.ExistsAsync(_owner.Id, _intro.Id, default));
        Assert.False(await repository.ExistsAsync(_owner.Id, _review.Id, default));
    }

    [Fact]
    public async Task Checks_slugs_with_an_optional_calendar_left_out()
    {
        await using var db = _database.CreateContext();
        var repository = new CalendarRepository(db);

        Assert.True(await repository.SlugExistsAsync("review", null, default));
        Assert.True(await repository.SlugExistsAsync("review", _intro.Id, default));
        Assert.False(await repository.SlugExistsAsync("review", _review.Id, default));
        Assert.False(await repository.SlugExistsAsync("free-slug", null, default));
    }

    [Fact]
    public async Task Saves_changes_to_a_found_calendar()
    {
        await using (var db = _database.CreateContext())
        {
            var repository = new CalendarRepository(db);
            var found = await repository.FindAsync(_owner.Id, _intro.Id, default);
            found!.SetMeetingLengths([new(30, 10, 10), new(60, 0, 0)]);
            found.SetWeeklyHours([new(DayOfWeek.Friday, new TimeOnly(14, 0), new TimeOnly(16, 0))]);
            found.Pause();
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var saved = await new CalendarRepository(check).FindAsync(_owner.Id, _intro.Id, default);
        Assert.True(saved!.IsPaused);
        Assert.Equal([30, 60], saved.MeetingLengths.Select(m => m.Minutes).Order());
        Assert.Equal(DayOfWeek.Friday, Assert.Single(saved.WeeklyHours).Day);
    }

    [Fact]
    public async Task Adds_and_removes_calendars()
    {
        var added = new Calendar(_owner.Id, "New", null, null, "new", [new(15, 0, 0)], Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new CalendarRepository(db);
            repository.Add(added);
            repository.Remove((await repository.FindAsync(_owner.Id, _coaching.Id, default))!);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(["intro-call", "new"], (await new CalendarRepository(check).ListAsync(_owner.Id, default)).Select(c => c.Slug).Order());
    }
}
