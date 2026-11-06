namespace BookingCalendars.Application.Calendars;

public interface ICalendarRepository
{
    /// <returns>The owner's calendars with their lengths and weekly hours, oldest first.</returns>
    Task<IReadOnlyList<Calendar>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <returns>A tracked calendar with its lengths and weekly hours, or <c>null</c> when the owner has no such calendar.</returns>
    Task<Calendar?> FindAsync(Guid ownerId, Guid calendarId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid ownerId, Guid calendarId, CancellationToken cancellationToken);

    /// <param name="exceptCalendarId">A calendar whose own slug does not count.</param>
    Task<bool> SlugExistsAsync(string slug, Guid? exceptCalendarId, CancellationToken cancellationToken);

    void Add(Calendar calendar);

    void Remove(Calendar calendar);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
