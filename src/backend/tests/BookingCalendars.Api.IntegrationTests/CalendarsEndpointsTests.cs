namespace BookingCalendars.Api.IntegrationTests;

public sealed class CalendarsEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Demo_owner_lists_the_seeded_calendars_with_their_public_links()
    {
        var client = await factory.LoggedInClientAsync("mila.laurent@example.com");

        var calendars = (await client.GetFromJsonAsync<List<CalendarResponse>>("/api/calendars"))!;

        Assert.Equal(["/book/mila-coaching-session", "/book/mila-intro-call"], calendars.Select(c => c.PublicPath).Order());
        var intro = calendars.Single(c => c.Slug == "mila-intro-call");
        Assert.Equal([new MeetingLengthDto(15, 5, 5), new MeetingLengthDto(30, 5, 10)], intro.MeetingLengths);
        Assert.Equal(new WeeklyHoursRangeDto(DayOfWeek.Monday, "09:00", "12:00"), intro.WeeklyHours[0]);
    }

    [Fact]
    public async Task Creates_reads_updates_and_deletes_a_calendar()
    {
        var client = await factory.RegisteredClientAsync("crud.owner@example.com");

        var created = await client.PostAsJsonAsync("/api/calendars", new CalendarRequest(
            "Portfolio review", "Bring your work.", "Video call", null, [new(60, 15, 15), new(30, 5, 5)]));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var calendar = (await created.Content.ReadFromJsonAsync<CalendarResponse>())!;
        Assert.Equal($"/api/calendars/{calendar.Id}", created.Headers.Location?.AbsolutePath);
        Assert.Equal("portfolio-review", calendar.Slug);
        Assert.Equal([30, 60], calendar.MeetingLengths.Select(m => m.Minutes));

        var read = await client.GetFromJsonAsync<CalendarResponse>($"/api/calendars/{calendar.Id}");
        Assert.Equal("Bring your work.", read?.Description);

        var updated = await client.PutAsJsonAsync($"/api/calendars/{calendar.Id}", new CalendarRequest(
            "Portfolio feedback", null, "Studio", "portfolio-feedback", [new(15, 0, 0)]));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = await updated.Content.ReadFromJsonAsync<CalendarResponse>();
        Assert.Equal("/book/portfolio-feedback", body?.PublicPath);
        Assert.Equal([new MeetingLengthDto(15, 0, 0)], body!.MeetingLengths);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/calendars/{calendar.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/calendars/{calendar.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/calendars/{calendar.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_calendar_returns_a_validation_problem()
    {
        var client = await factory.RegisteredClientAsync("invalid.owner@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/calendars", new CalendarRequest(" ", null, null, "mila-intro-call", [new(45, 0, 0)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["meetingLengths", "slug", "title"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Pauses_and_resumes_a_calendar()
    {
        var client = await factory.RegisteredClientAsync("pause.owner@example.com");
        var calendar = await client.CreateCalendarAsync("Pause test");

        var paused = await client.PutAsJsonAsync($"/api/calendars/{calendar.Id}/paused", new PauseCalendarRequest(true));

        Assert.True((await paused.Content.ReadFromJsonAsync<CalendarResponse>())?.IsPaused);
        Assert.True((await client.GetFromJsonAsync<CalendarResponse>($"/api/calendars/{calendar.Id}"))?.IsPaused);
        var resumed = await client.PutAsJsonAsync($"/api/calendars/{calendar.Id}/paused", new PauseCalendarRequest(false));
        Assert.False((await resumed.Content.ReadFromJsonAsync<CalendarResponse>())?.IsPaused);
    }

    [Fact]
    public async Task Sets_weekly_hours_and_refuses_overlaps()
    {
        var client = await factory.RegisteredClientAsync("hours.owner@example.com");
        var calendar = await client.CreateCalendarAsync("Hours test");

        var saved = await client.PutAsJsonAsync($"/api/calendars/{calendar.Id}/weekly-hours", new WeeklyHoursRequest(
            [new(DayOfWeek.Wednesday, "13:00", "15:00"), new(DayOfWeek.Wednesday, "09:00", "12:00")]));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(
            ["09:00", "13:00"],
            (await saved.Content.ReadFromJsonAsync<CalendarResponse>())!.WeeklyHours.Select(r => r.Start));

        var refused = await client.PutAsJsonAsync($"/api/calendars/{calendar.Id}/weekly-hours", new WeeklyHoursRequest(
            [new(DayOfWeek.Wednesday, "09:00", "12:00"), new(DayOfWeek.Wednesday, "11:30", "12:30")]));
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["Wednesday: 09:00-12:00 and 11:30-12:30 overlap."], problem!.Errors["ranges"]);
    }

    [Fact]
    public async Task Calendars_of_other_owners_are_hidden()
    {
        var owner = await factory.LoggedInClientAsync("owen.hart@example.com");
        var ownCalendar = (await owner.GetFromJsonAsync<List<CalendarResponse>>("/api/calendars"))!.First();
        var stranger = await factory.RegisteredClientAsync("stranger@example.com");

        Assert.Empty((await stranger.GetFromJsonAsync<List<CalendarResponse>>("/api/calendars"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/calendars/{ownCalendar.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync(
            $"/api/calendars/{ownCalendar.Id}", new CalendarRequest("Taken", null, null, null, [new(30, 0, 0)]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync(
            $"/api/calendars/{ownCalendar.Id}/paused", new PauseCalendarRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync(
            $"/api/calendars/{ownCalendar.Id}/weekly-hours", new WeeklyHoursRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/calendars/{ownCalendar.Id}")).StatusCode);
    }

    [Fact]
    public async Task Calendar_endpoints_need_a_known_user()
    {
        var anonymous = factory.CreateClient();
        await AssertAllRefused(anonymous);

        var withoutSubject = factory.CreateClient();
        withoutSubject.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenWithoutSubject());
        await AssertAllRefused(withoutSubject);

        const string email = "deleted.owner@example.com";
        var deleted = await factory.RegisteredClientAsync(email);
        await factory.DeleteUserAsync(email);
        await AssertAllRefused(deleted);
    }

    private static async Task AssertAllRefused(HttpClient client)
    {
        var id = Guid.NewGuid();
        var request = new CalendarRequest("Title", null, null, null, [new(30, 0, 0)]);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/calendars")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/calendars/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/calendars", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"/api/calendars/{id}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync(
            $"/api/calendars/{id}/paused", new PauseCalendarRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync(
            $"/api/calendars/{id}/weekly-hours", new WeeklyHoursRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/calendars/{id}")).StatusCode);
    }
}
