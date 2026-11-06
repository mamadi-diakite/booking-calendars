namespace BookingCalendars.Api.IntegrationTests;

internal static class ApiClients
{
    public const string DemoPassword = "DemoPass123!";

    /// <summary>Registers a fresh account so each test works on its own calendars.</summary>
    public static async Task<HttpClient> RegisteredClientAsync(this WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Test User", email, DemoPassword, DemoPassword, "Europe/Paris"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    public static async Task<HttpClient> LoggedInClientAsync(this WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DemoPassword));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    public static string TokenWithoutSubject(this WebApplicationFactory<Program> factory)
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }

    public static async Task DeleteUserAsync(this WebApplicationFactory<Program> factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingCalendarsDbContext>();
        db.Users.Remove(db.Users.Single(u => u.Email == email));
        await db.SaveChangesAsync();
    }

    public static async Task<CalendarResponse> CreateCalendarAsync(this HttpClient client, string title, string? slug = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/calendars", new CalendarRequest(title, null, null, slug, [new MeetingLengthDto(30, 5, 5)]));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CalendarResponse>())!;
    }
}
