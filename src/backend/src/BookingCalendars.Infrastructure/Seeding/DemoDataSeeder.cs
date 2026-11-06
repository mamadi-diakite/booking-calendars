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

    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var passwordHash = passwordHasher.Hash(DemoPassword);

        foreach (var account in Owners.Concat(Bookers))
        {
            db.Users.Add(new User(account.Email, account.DisplayName, passwordHash, account.TimeZoneId, now));
        }

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed record DemoAccount(string Email, string DisplayName, string TimeZoneId);
