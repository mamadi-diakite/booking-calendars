namespace BookingCalendars.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_blank_connection_string(string connectionString)
    {
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddInfrastructure(connectionString));
    }

    [Fact]
    public async Task Registers_the_infrastructure_services()
    {
        await using var provider = new ServiceCollection()
            .AddInfrastructure("Data Source=:memory:")
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        Assert.Same(TimeProvider.System, scope.ServiceProvider.GetRequiredService<TimeProvider>());
        Assert.IsType<IdentityPasswordHasher>(scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
        Assert.IsType<UserRepository>(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.IsType<CalendarRepository>(scope.ServiceProvider.GetRequiredService<ICalendarRepository>());
        Assert.IsType<BlockedPeriodRepository>(scope.ServiceProvider.GetRequiredService<IBlockedPeriodRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DemoDataSeeder>());
    }

    [Fact]
    public void Keeps_a_time_provider_registered_before()
    {
        var fixedTime = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        using var provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(fixedTime)
            .AddInfrastructure("Data Source=:memory:")
            .BuildServiceProvider();

        Assert.Same(fixedTime, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public async Task Seeds_demo_data_through_the_service_provider()
    {
        await using var provider = new ServiceCollection()
            .AddInfrastructure("Data Source=seed-test;Mode=Memory;Cache=Shared")
            .BuildServiceProvider();
        await using var keepAlive = new SqliteConnection("Data Source=seed-test;Mode=Memory;Cache=Shared");
        await keepAlive.OpenAsync();
        await provider.ApplyMigrationsAsync();

        Assert.True(await provider.SeedDemoDataAsync());
        Assert.False(await provider.SeedDemoDataAsync());
    }

    [Fact]
    public async Task Creates_missing_data_folder_and_applies_migrations()
    {
        var root = Path.Combine(Path.GetTempPath(), "booking-calendars-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "data", "app.db");

        try
        {
            await using var provider = new ServiceCollection()
                .AddInfrastructure($"Data Source={databasePath}")
                .BuildServiceProvider();

            await provider.ApplyMigrationsAsync();

            Assert.True(File.Exists(databasePath));
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingCalendarsDbContext>();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=in-memory-test;Mode=Memory;Cache=Shared")]
    public async Task Applies_migrations_to_in_memory_database_without_touching_disk(string connectionString)
    {
        await using var provider = new ServiceCollection()
            .AddInfrastructure(connectionString)
            .BuildServiceProvider();

        await provider.ApplyMigrationsAsync();

        Assert.False(File.Exists("in-memory-test"));
    }
}
