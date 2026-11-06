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
