namespace BookingCalendars.Infrastructure.Seeding;

public sealed class DemoDataSeeder(BookingCalendarsDbContext db, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "DemoPass123!";

    /// <summary>Fictional people who share booking calendars. All names are made up.</summary>
    public static IReadOnlyList<DemoAccount> Owners { get; } =
    [
        new("mila.laurent@example.com", "Mila Laurent", "Europe/Paris"),
        new("owen.hart@example.com", "Owen Hart", "America/New_York"),
    ];

    /// <summary>Fictional people who book slots on the owners' calendars.</summary>
    public static IReadOnlyList<DemoAccount> Bookers { get; } =
    [
        new("ines.moreau@example.com", "Ines Moreau", "Europe/Paris"),
        new("theo.brandt@example.com", "Theo Brandt", "Europe/Berlin"),
    ];

    public static IReadOnlyList<DemoCalendar> Calendars { get; } =
    [
        new(
            "mila.laurent@example.com",
            "Intro call",
            "mila-intro-call",
            "A short first talk to see how I can help.",
            "Video call, the link comes with the confirmation.",
            [new(15, 5, 5), new(30, 5, 10)],
            WorkingDays(new TimeOnly(9, 0), new TimeOnly(12, 0), new TimeOnly(14, 0), new TimeOnly(17, 0))),
        new(
            "mila.laurent@example.com",
            "Coaching session",
            "mila-coaching-session",
            "A full hour on one topic of your choice.",
            "Studio 4, 12 Example Street.",
            [new(60, 15, 15)],
            [
                new(DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(16, 0)),
                new(DayOfWeek.Thursday, new TimeOnly(10, 0), new TimeOnly(16, 0)),
            ]),
        new(
            "owen.hart@example.com",
            "Project review",
            "owen-project-review",
            "We go through your project plan together.",
            null,
            [new(30, 0, 10), new(60, 10, 15)],
            [
                new(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(18, 0)),
                new(DayOfWeek.Wednesday, new TimeOnly(13, 0), new TimeOnly(18, 0)),
                new(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            ]),
    ];

    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var passwordHash = passwordHasher.Hash(DemoPassword);

        var users = Owners.Concat(Bookers)
            .Select(account => new User(account.Email, account.DisplayName, passwordHash, account.TimeZoneId, now))
            .ToDictionary(u => u.Email);
        db.Users.AddRange(users.Values);

        foreach (var demo in Calendars)
        {
            var calendar = new Calendar(
                users[demo.OwnerEmail].Id, demo.Title, demo.Description, demo.LocationNote, demo.Slug, demo.MeetingLengths, now);
            calendar.SetWeeklyHours(demo.WeeklyHours);
            db.Calendars.Add(calendar);
        }

        // Relative to today, so the demo always shows upcoming blocked days.
        var today = DateOnly.FromDateTime(now);
        var mila = users["mila.laurent@example.com"];
        db.BlockedPeriods.Add(new BlockedPeriod(mila.Id, null, today.AddDays(21), today.AddDays(25), "Holidays", now));
        db.BlockedPeriods.Add(new BlockedPeriod(
            mila.Id,
            db.Calendars.Local.Single(c => c.Slug == "mila-coaching-session").Id,
            today.AddDays(9),
            today.AddDays(9),
            "Training day",
            now));

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static WeeklyHoursSettings[] WorkingDays(TimeOnly morningStart, TimeOnly morningEnd, TimeOnly afternoonStart, TimeOnly afternoonEnd) =>
    [
        .. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
            .SelectMany(day => new[]
            {
                new WeeklyHoursSettings(day, morningStart, morningEnd),
                new WeeklyHoursSettings(day, afternoonStart, afternoonEnd),
            }),
    ];
}

public sealed record DemoAccount(string Email, string DisplayName, string TimeZoneId);

public sealed record DemoCalendar(
    string OwnerEmail,
    string Title,
    string Slug,
    string? Description,
    string? LocationNote,
    IReadOnlyList<MeetingLengthSettings> MeetingLengths,
    IReadOnlyList<WeeklyHoursSettings> WeeklyHours);
