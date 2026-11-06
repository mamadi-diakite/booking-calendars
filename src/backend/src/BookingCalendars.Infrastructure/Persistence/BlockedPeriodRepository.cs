namespace BookingCalendars.Infrastructure.Persistence;

public sealed class BlockedPeriodRepository(BookingCalendarsDbContext db) : IBlockedPeriodRepository
{
    public async Task<IReadOnlyList<BlockedPeriod>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.BlockedPeriods
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.StartDate)
            .ThenBy(p => p.EndDate)
            .ToListAsync(cancellationToken);

    public Task<BlockedPeriod?> FindAsync(Guid ownerId, Guid periodId, CancellationToken cancellationToken) =>
        db.BlockedPeriods.SingleOrDefaultAsync(p => p.Id == periodId && p.OwnerId == ownerId, cancellationToken);

    public void Add(BlockedPeriod period) => db.BlockedPeriods.Add(period);

    public void Remove(BlockedPeriod period) => db.BlockedPeriods.Remove(period);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
