namespace BookingCalendars.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CalendarService>();
        services.AddScoped<BlockedPeriodService>();

        return services;
    }
}
