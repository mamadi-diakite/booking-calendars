namespace BookingCalendars.Infrastructure.Tests;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _user = new("mila.laurent@example.com", "Mila", "hash", "Europe/Paris", Now);

    public UserRepositoryTests()
    {
        using var db = _database.CreateContext();
        db.Users.Add(_user);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Finds_a_user_by_email_and_by_id()
    {
        await using var db = _database.CreateContext();
        var repository = new UserRepository(db);

        Assert.Equal(_user.Id, (await repository.FindByEmailAsync("mila.laurent@example.com", default))?.Id);
        Assert.Equal(_user.Email, (await repository.FindByIdAsync(_user.Id, default))?.Email);
        Assert.Null(await repository.FindByEmailAsync("nobody@example.com", default));
        Assert.Null(await repository.FindByIdAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Adds_a_user_and_saves_a_changed_password_and_time_zone()
    {
        var booker = new User("ines@example.com", "Ines", "hash", "UTC", Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new UserRepository(db);
            repository.Add(booker);
            var existing = await repository.FindByIdAsync(_user.Id, default);
            existing!.ChangePassword("new-hash");
            existing.ChangeTimeZone("Asia/Singapore");
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.True(await check.Users.AnyAsync(u => u.Id == booker.Id));
        var saved = await check.Users.SingleAsync(u => u.Id == _user.Id);
        Assert.Equal("new-hash", saved.PasswordHash);
        Assert.Equal("Asia/Singapore", saved.TimeZoneId);
    }
}
