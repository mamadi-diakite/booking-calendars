namespace BookingCalendars.Api.IntegrationTests;

public sealed class BlockedPeriodsEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private static readonly DateOnly Day = new(2030, 3, 4);

    [Fact]
    public async Task Demo_owner_lists_the_seeded_periods()
    {
        var client = await factory.LoggedInClientAsync("mila.laurent@example.com");

        var periods = (await client.GetFromJsonAsync<List<BlockedPeriodResponse>>("/api/blocked-periods"))!;

        Assert.Equal(["Training day", "Holidays"], periods.Select(p => p.Label));
        Assert.NotNull(periods[0].CalendarId);
        Assert.Null(periods[1].CalendarId);
    }

    [Fact]
    public async Task Blocks_and_unblocks_days()
    {
        var client = await factory.RegisteredClientAsync("blocking.owner@example.com");
        var calendar = await client.CreateCalendarAsync("Blocking test");

        var onOne = await client.PostAsJsonAsync("/api/blocked-periods", new BlockedPeriodRequest(calendar.Id, Day, Day, "Dentist"));
        var onAll = await client.PostAsJsonAsync(
            "/api/blocked-periods", new BlockedPeriodRequest(null, Day.AddDays(5), Day.AddDays(9), "Holidays"));

        Assert.Equal(HttpStatusCode.Created, onOne.StatusCode);
        var created = (await onOne.Content.ReadFromJsonAsync<BlockedPeriodResponse>())!;
        Assert.Equal($"/api/blocked-periods/{created.Id}", onOne.Headers.Location?.OriginalString);
        Assert.Equal(new BlockedPeriodResponse(created.Id, calendar.Id, Day, Day, "Dentist"), created);
        Assert.Equal(HttpStatusCode.Created, onAll.StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<List<BlockedPeriodResponse>>("/api/blocked-periods"))!.Count);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/blocked-periods/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/blocked-periods/{created.Id}")).StatusCode);
        Assert.Equal(["Holidays"], (await client.GetFromJsonAsync<List<BlockedPeriodResponse>>("/api/blocked-periods"))!
            .Select(p => p.Label));
    }

    [Fact]
    public async Task Dates_travel_as_iso_days()
    {
        var client = await factory.RegisteredClientAsync("dates.owner@example.com");

        var response = await client.PostAsJsonAsync("/api/blocked-periods", new { startDate = "2030-05-01", endDate = "2030-05-02" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"startDate\":\"2030-05-01\"", json, StringComparison.Ordinal);
        Assert.Contains("\"endDate\":\"2030-05-02\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_period_returns_a_validation_problem()
    {
        var client = await factory.RegisteredClientAsync("invalid.blocking@example.com");
        await client.PostAsJsonAsync("/api/blocked-periods", new BlockedPeriodRequest(null, Day, Day, null));

        var reversed = await client.PostAsJsonAsync("/api/blocked-periods", new BlockedPeriodRequest(Guid.NewGuid(), Day, Day.AddDays(-1), null));
        var overlapping = await client.PostAsJsonAsync("/api/blocked-periods", new BlockedPeriodRequest(null, Day, Day, null));

        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal(["calendarId", "endDate"], (await reversed.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Order());
        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);
        Assert.Equal(["startDate"], (await overlapping.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task Periods_of_other_owners_are_hidden()
    {
        var owner = await factory.LoggedInClientAsync("mila.laurent@example.com");
        var period = (await owner.GetFromJsonAsync<List<BlockedPeriodResponse>>("/api/blocked-periods"))!.First();
        var stranger = await factory.RegisteredClientAsync("blocking.stranger@example.com");

        Assert.Empty((await stranger.GetFromJsonAsync<List<BlockedPeriodResponse>>("/api/blocked-periods"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/blocked-periods/{period.Id}")).StatusCode);
    }

    [Fact]
    public async Task Blocked_period_endpoints_need_a_known_user()
    {
        await AssertAllRefused(factory.CreateClient());

        var withoutSubject = factory.CreateClient();
        withoutSubject.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenWithoutSubject());
        await AssertAllRefused(withoutSubject);

        const string email = "deleted.blocking@example.com";
        var deleted = await factory.RegisteredClientAsync(email);
        await factory.DeleteUserAsync(email);
        await AssertAllRefused(deleted);
    }

    private static async Task AssertAllRefused(HttpClient client)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/blocked-periods")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/blocked-periods", new BlockedPeriodRequest(null, Day, Day, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/blocked-periods/{Guid.NewGuid()}")).StatusCode);
    }
}
