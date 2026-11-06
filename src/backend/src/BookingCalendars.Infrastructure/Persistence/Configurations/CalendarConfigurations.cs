namespace BookingCalendars.Infrastructure.Persistence.Configurations;

internal sealed class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
{
    public void Configure(EntityTypeBuilder<Calendar> builder)
    {
        builder.ToTable("Calendars");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).HasMaxLength(Calendar.TitleMaxLength).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(Calendar.DescriptionMaxLength);
        builder.Property(c => c.LocationNote).HasMaxLength(Calendar.LocationNoteMaxLength);
        builder.Property(c => c.Slug).HasMaxLength(CalendarSlug.MaxLength).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.OwnerId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.MeetingLengths)
            .WithOne()
            .HasForeignKey(m => m.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.WeeklyHours)
            .WithOne()
            .HasForeignKey(r => r.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CalendarMeetingLengthConfiguration : IEntityTypeConfiguration<CalendarMeetingLength>
{
    public void Configure(EntityTypeBuilder<CalendarMeetingLength> builder)
    {
        builder.ToTable("CalendarMeetingLengths");
        builder.HasKey(m => m.Id);

        // Ids are set by the domain, so new lengths added to a loaded calendar are inserted.
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.HasIndex(m => new { m.CalendarId, m.Minutes }).IsUnique();
    }
}

internal sealed class WeeklyHoursRangeConfiguration : IEntityTypeConfiguration<WeeklyHoursRange>
{
    public void Configure(EntityTypeBuilder<WeeklyHoursRange> builder)
    {
        builder.ToTable("WeeklyHours");
        builder.HasKey(r => r.Id);

        // Ids are set by the domain, so new ranges added to a loaded calendar are inserted.
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Day).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.HasIndex(r => r.CalendarId);
    }
}

internal sealed class BlockedPeriodConfiguration : IEntityTypeConfiguration<BlockedPeriod>
{
    public void Configure(EntityTypeBuilder<BlockedPeriod> builder)
    {
        builder.ToTable("BlockedPeriods");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Label).HasMaxLength(BlockedPeriod.LabelMaxLength);
        builder.HasIndex(p => new { p.OwnerId, p.StartDate });
        builder.Ignore(p => p.AppliesToAllCalendars);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Calendar>()
            .WithMany()
            .HasForeignKey(p => p.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
