namespace BookingCalendars.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/blocked-periods")]
public sealed class BlockedPeriodsController(BlockedPeriodService blockedPeriodService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlockedPeriodResponse>>> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await blockedPeriodService.ListAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<BlockedPeriodResponse>> Create(BlockedPeriodRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await blockedPeriodService.CreateAsync(userId, request, cancellationToken);

        return result.Status == CalendarResultStatus.Ok
            ? Created($"/api/blocked-periods/{result.Value!.Id}", result.Value)
            : this.ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return this.ToActionResult(await blockedPeriodService.DeleteAsync(userId, id, cancellationToken));
    }
}
