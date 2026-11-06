namespace BookingCalendars.Application.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void Registers_the_application_services()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IUserRepository, InMemoryUserRepository>()
            .AddSingleton<ICalendarRepository, InMemoryCalendarRepository>()
            .AddSingleton<IBlockedPeriodRepository, InMemoryBlockedPeriodRepository>()
            .AddSingleton<IPasswordHasher, FakePasswordHasher>()
            .AddSingleton<IAccessTokenIssuer, FakeTokenIssuer>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<AuthService>());
        Assert.NotNull(scope.ServiceProvider.GetService<CalendarService>());
        Assert.NotNull(scope.ServiceProvider.GetService<BlockedPeriodService>());
    }
}
