namespace BookingCalendars.Infrastructure.Persistence;

public sealed class BookingCalendarsDbContext(DbContextOptions<BookingCalendarsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Calendar> Calendars => Set<Calendar>();

    public DbSet<CalendarMeetingLength> CalendarMeetingLengths => Set<CalendarMeetingLength>();

    public DbSet<WeeklyHoursRange> WeeklyHours => Set<WeeklyHoursRange>();

    public DbSet<BlockedPeriod> BlockedPeriods => Set<BlockedPeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingCalendarsDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
