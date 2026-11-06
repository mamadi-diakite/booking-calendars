namespace BookingCalendars.Infrastructure.Tests;

public sealed class DemoDataSeederTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly IdentityPasswordHasher _hasher = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeds_owners_and_bookers_with_their_time_zones()
    {
        Assert.True(await SeedAsync());

        await using var db = _database.CreateContext();
        var users = await db.Users.OrderBy(u => u.Email).ToListAsync();

        var expected = DemoDataSeeder.Owners.Concat(DemoDataSeeder.Bookers).OrderBy(a => a.Email).ToList();
        Assert.Equal(expected.Select(a => a.Email), users.Select(u => u.Email));
        Assert.Equal(expected.Select(a => a.TimeZoneId), users.Select(u => u.TimeZoneId));
        Assert.Equal(expected.Select(a => a.DisplayName), users.Select(u => u.DisplayName));
        Assert.All(users, u => Assert.EndsWith("@example.com", u.Email));
        Assert.All(users, u => Assert.Equal(Now, u.CreatedAtUtc));
    }

    [Fact]
    public async Task Seeds_the_owners_calendars_with_their_rules()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        var calendars = await db.Calendars.Include(c => c.MeetingLengths).Include(c => c.WeeklyHours).ToListAsync();
        var owners = await db.Users.Where(u => DemoDataSeeder.Owners.Select(o => o.Email).Contains(u.Email)).ToListAsync();

        Assert.Equal(DemoDataSeeder.Calendars.Select(c => c.Slug).Order(), calendars.Select(c => c.Slug).Order());
        Assert.All(calendars, c => Assert.Contains(owners, o => o.Id == c.OwnerId));
        Assert.All(calendars, c => Assert.NotEmpty(c.MeetingLengths));
        Assert.All(calendars, c => Assert.NotEmpty(c.WeeklyHours));
        Assert.All(calendars, c => Assert.False(c.IsPaused));
        var intro = calendars.Single(c => c.Slug == "mila-intro-call");
        Assert.Equal(10, intro.WeeklyHours.Count);
        Assert.Equal([15, 30], intro.MeetingLengths.Select(m => m.Minutes).Order());
    }

    [Fact]
    public async Task Seeds_upcoming_blocked_periods_on_one_and_all_calendars()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        var periods = await db.BlockedPeriods.OrderBy(p => p.StartDate).ToListAsync();
        var coaching = await db.Calendars.SingleAsync(c => c.Slug == "mila-coaching-session");

        Assert.Equal(2, periods.Count);
        Assert.Equal(coaching.Id, periods[0].CalendarId);
        Assert.Equal(new DateOnly(2026, 11, 15), periods[0].StartDate);
        Assert.Null(periods[1].CalendarId);
        Assert.Equal(new DateOnly(2026, 11, 27), periods[1].StartDate);
        Assert.Equal(new DateOnly(2026, 12, 1), periods[1].EndDate);
    }

    [Fact]
    public async Task Every_demo_account_accepts_the_demo_password()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        foreach (var user in await db.Users.ToListAsync())
        {
            Assert.True(_hasher.Verify(user.PasswordHash, DemoDataSeeder.DemoPassword), user.Email);
        }
    }

    [Fact]
    public async Task Second_run_adds_nothing()
    {
        await SeedAsync();

        Assert.False(await SeedAsync());

        await using var db = _database.CreateContext();
        Assert.Equal(DemoDataSeeder.Owners.Count + DemoDataSeeder.Bookers.Count, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Skips_a_database_that_already_has_users()
    {
        await using (var db = _database.CreateContext())
        {
            db.Users.Add(new User("someone@example.com", "Someone", "hash", "UTC", Now));
            await db.SaveChangesAsync();
        }

        Assert.False(await SeedAsync());

        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.Users.CountAsync());
    }

    private async Task<bool> SeedAsync()
    {
        await using var db = _database.CreateContext();

        return await new DemoDataSeeder(db, _hasher, new FixedTimeProvider(Now)).SeedAsync();
    }
}
