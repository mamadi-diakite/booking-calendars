namespace BookingCalendars.Infrastructure.Persistence;

public sealed class CalendarRepository(BookingCalendarsDbContext db) : ICalendarRepository
{
    public async Task<IReadOnlyList<Calendar>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Calendars
            .AsNoTracking()
            .Include(c => c.MeetingLengths)
            .Include(c => c.WeeklyHours)
            .Where(c => c.OwnerId == ownerId)
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Title)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<Calendar?> FindAsync(Guid ownerId, Guid calendarId, CancellationToken cancellationToken) =>
        db.Calendars
            .Include(c => c.MeetingLengths)
            .Include(c => c.WeeklyHours)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.Id == calendarId && c.OwnerId == ownerId, cancellationToken);

    public Task<bool> ExistsAsync(Guid ownerId, Guid calendarId, CancellationToken cancellationToken) =>
        db.Calendars.AnyAsync(c => c.Id == calendarId && c.OwnerId == ownerId, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptCalendarId, CancellationToken cancellationToken) =>
        db.Calendars.AnyAsync(c => c.Slug == slug && c.Id != exceptCalendarId, cancellationToken);

    public void Add(Calendar calendar) => db.Calendars.Add(calendar);

    public void Remove(Calendar calendar) => db.Calendars.Remove(calendar);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
