namespace BookingCalendars.Api.IntegrationTests;

public sealed class AuthEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private const string DemoPassword = ApiClients.DemoPassword;

    [Fact]
    public async Task Register_creates_a_user_and_returns_a_session()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Jade Roux", "jade.roux@example.com", "NewPass456", "NewPass456", "Europe/Brussels"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/auth/me", response.Headers.Location?.AbsolutePath);
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("jade.roux@example.com", session?.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("Jade Roux", me?.DisplayName);
        Assert.Equal("Europe/Brussels", me?.TimeZone);
    }

    [Fact]
    public async Task Register_with_errors_returns_a_validation_problem()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(" ", "mila.laurent@example.com", "short", "other", "Moon/Base"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "displayName", "email", "password", "timeZone"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Login_with_a_demo_account_returns_a_token()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest("Mila.Laurent@Example.com", DemoPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken));
        Assert.Equal("mila.laurent@example.com", body!.User.Email);
        Assert.Equal("Europe/Paris", body.User.TimeZone);
    }

    [Theory]
    [InlineData("mila.laurent@example.com", "WrongPass123")]
    [InlineData("nobody@example.com", DemoPassword)]
    [InlineData("", "")]
    public async Task Login_with_bad_credentials_returns_401(string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email or password is not correct.", problem?.Title);
    }

    [Fact]
    public async Task Me_returns_the_logged_in_user()
    {
        var client = await factory.LoggedInClientAsync("theo.brandt@example.com");

        var user = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.Equal("theo.brandt@example.com", user?.Email);
        Assert.Equal("Europe/Berlin", user?.TimeZone);
    }

    [Fact]
    public async Task Protected_endpoints_need_a_token()
    {
        await AssertProtectedEndpointsRefuse(factory.CreateClient());
    }

    [Fact]
    public async Task A_token_without_a_user_id_is_refused()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenWithoutSubject());

        await AssertProtectedEndpointsRefuse(client);
    }

    [Fact]
    public async Task A_token_of_a_deleted_user_is_refused()
    {
        const string email = "temporary@example.com";
        var client = await factory.RegisteredClientAsync(email);
        await factory.DeleteUserAsync(email);

        await AssertProtectedEndpointsRefuse(client);
    }

    [Fact]
    public async Task Change_password_with_errors_returns_a_validation_problem()
    {
        var client = await factory.LoggedInClientAsync("ines.moreau@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest("WrongPass789", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Changed_password_replaces_the_old_one()
    {
        var client = await factory.RegisteredClientAsync("lea.marchand@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("lea.marchand@example.com", DemoPassword))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("lea.marchand@example.com", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task Changed_time_zone_is_returned_and_kept()
    {
        var client = await factory.RegisteredClientAsync("sam.okafor@example.com");

        var response = await client.PutAsJsonAsync("/api/auth/time-zone", new ChangeTimeZoneRequest("Africa/Lagos"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Africa/Lagos", (await response.Content.ReadFromJsonAsync<CurrentUserResponse>())?.TimeZone);
        Assert.Equal("Africa/Lagos", (await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me"))?.TimeZone);
    }

    [Fact]
    public async Task Unknown_time_zone_returns_a_validation_problem()
    {
        var client = await factory.RegisteredClientAsync("nina.vos@example.com");

        var response = await client.PutAsJsonAsync("/api/auth/time-zone", new ChangeTimeZoneRequest("Lagos"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["timeZone"], problem!.Errors.Keys);
    }

    private static async Task AssertProtectedEndpointsRefuse(HttpClient client)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync(
            "/api/auth/time-zone", new ChangeTimeZoneRequest("UTC"))).StatusCode);
    }
}
