namespace BookingCalendars.Application.Calendars;

public interface IBlockedPeriodRepository
{
    /// <returns>Every blocked period of the owner, by start date.</returns>
    Task<IReadOnlyList<BlockedPeriod>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<BlockedPeriod?> FindAsync(Guid ownerId, Guid periodId, CancellationToken cancellationToken);

    void Add(BlockedPeriod period);

    void Remove(BlockedPeriod period);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
