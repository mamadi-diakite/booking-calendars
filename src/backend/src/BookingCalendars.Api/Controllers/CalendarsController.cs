namespace BookingCalendars.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendars")]
public sealed class CalendarsController(CalendarService calendarService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarResponse>>> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await calendarService.ListAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CalendarResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await calendarService.GetAsync(userId, id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CalendarResponse>> Create(CalendarRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await calendarService.CreateAsync(userId, request, cancellationToken);

        return result.Status == CalendarResultStatus.Ok
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : this.ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CalendarResponse>> Update(Guid id, CalendarRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await calendarService.UpdateAsync(userId, id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/paused")]
    public async Task<ActionResult<CalendarResponse>> SetPaused(
        Guid id,
        PauseCalendarRequest request,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await calendarService.SetPausedAsync(userId, id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/weekly-hours")]
    public async Task<ActionResult<CalendarResponse>> SetWeeklyHours(
        Guid id,
        WeeklyHoursRequest request,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await calendarService.SetWeeklyHoursAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await calendarService.DeleteAsync(userId, id, cancellationToken));
    }
}
