namespace BookingCalendars.Infrastructure.Tests;

public sealed class BlockedPeriodRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Day = new(2026, 12, 24);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _owner = new("mila@example.com", "Mila", "hash", "Europe/Paris", Now);
    private readonly User _other = new("owen@example.com", "Owen", "hash", "UTC", Now);
    private readonly BlockedPeriod _later;
    private readonly BlockedPeriod _sooner;
    private readonly BlockedPeriod _othersPeriod;

    public BlockedPeriodRepositoryTests()
    {
        _later = new BlockedPeriod(_owner.Id, null, Day.AddDays(7), Day.AddDays(8), "Later", Now);
        _sooner = new BlockedPeriod(_owner.Id, null, Day, Day, "Sooner", Now);
        _othersPeriod = new BlockedPeriod(_other.Id, null, Day, Day, "Not mine", Now);

        using var db = _database.CreateContext();
        db.Users.AddRange(_owner, _other);
        db.BlockedPeriods.AddRange(_later, _sooner, _othersPeriod);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_the_owners_periods_by_start_date()
    {
        await using var db = _database.CreateContext();

        var list = await new BlockedPeriodRepository(db).ListAsync(_owner.Id, default);

        Assert.Equal(["Sooner", "Later"], list.Select(p => p.Label));
    }

    [Fact]
    public async Task Finds_only_the_owners_period()
    {
        await using var db = _database.CreateContext();
        var repository = new BlockedPeriodRepository(db);

        Assert.Equal("Later", (await repository.FindAsync(_owner.Id, _later.Id, default))?.Label);
        Assert.Null(await repository.FindAsync(_owner.Id, _othersPeriod.Id, default));
    }

    [Fact]
    public async Task Adds_and_removes_periods()
    {
        var added = new BlockedPeriod(_owner.Id, null, Day.AddDays(30), Day.AddDays(30), "Added", Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new BlockedPeriodRepository(db);
            repository.Add(added);
            repository.Remove((await repository.FindAsync(_owner.Id, _sooner.Id, default))!);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(["Later", "Added"], (await new BlockedPeriodRepository(check).ListAsync(_owner.Id, default)).Select(p => p.Label));
    }
}
