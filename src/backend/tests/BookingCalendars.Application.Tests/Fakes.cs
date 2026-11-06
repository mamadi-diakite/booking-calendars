namespace BookingCalendars.Application.Tests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public void Add(User user) => Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryCalendarRepository : ICalendarRepository
{
    public List<Calendar> Calendars { get; } = [];

    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<Calendar>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Calendar>>([.. Calendars.Where(c => c.OwnerId == ownerId).OrderBy(c => c.CreatedAtUtc)]);

    public Task<Calendar?> FindAsync(Guid ownerId, Guid calendarId, CancellationToken cancellationToken) =>
        Task.FromResult(Calendars.SingleOrDefault(c => c.Id == calendarId && c.OwnerId == ownerId));

    public Task<bool> ExistsAsync(Guid ownerId, Guid calendarId, CancellationToken cancellationToken) =>
        Task.FromResult(Calendars.Any(c => c.Id == calendarId && c.OwnerId == ownerId));

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptCalendarId, CancellationToken cancellationToken) =>
        Task.FromResult(Calendars.Any(c => c.Slug == slug && c.Id != exceptCalendarId));

    public void Add(Calendar calendar) => Calendars.Add(calendar);

    public void Remove(Calendar calendar) => Calendars.Remove(calendar);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryBlockedPeriodRepository : IBlockedPeriodRepository
{
    public List<BlockedPeriod> Periods { get; } = [];

    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<BlockedPeriod>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BlockedPeriod>>([.. Periods.Where(p => p.OwnerId == ownerId).OrderBy(p => p.StartDate)]);

    public Task<BlockedPeriod?> FindAsync(Guid ownerId, Guid periodId, CancellationToken cancellationToken) =>
        Task.FromResult(Periods.SingleOrDefault(p => p.Id == periodId && p.OwnerId == ownerId));

    public void Add(BlockedPeriod period) => Periods.Add(period);

    public void Remove(BlockedPeriod period) => Periods.Remove(period);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Readable fake: the hash is the password with a prefix.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCount { get; private set; }

    public string Hash(string password)
    {
        HashCount++;
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAt = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    public AccessToken Issue(User user) => new($"token-for-{user.Id}", ExpiresAt);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
