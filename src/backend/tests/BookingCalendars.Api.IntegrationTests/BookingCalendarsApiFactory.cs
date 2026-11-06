namespace BookingCalendars.Api.IntegrationTests;

/// <summary>Runs the API against its own throwaway SQLite file.</summary>
public sealed class BookingCalendarsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), "booking-calendars-tests", $"{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
