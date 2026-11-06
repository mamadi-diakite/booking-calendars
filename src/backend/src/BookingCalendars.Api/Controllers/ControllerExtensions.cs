namespace BookingCalendars.Api.Controllers;

internal static class ControllerExtensions
{
    /// <summary>Returns a validation problem with the messages of each request field.</summary>
    public static ActionResult InvalidRequest(this ControllerBase controller, IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                controller.ModelState.AddModelError(field, message);
            }
        }

        return controller.ValidationProblem();
    }

    public static ActionResult ToActionResult<T>(this ControllerBase controller, CalendarResult<T> result)
        where T : class => result.Status switch
        {
            CalendarResultStatus.Ok => controller.Ok(result.Value),
            CalendarResultStatus.Invalid => controller.InvalidRequest(result.Errors),
            _ => controller.ToActionResult(result.Status),
        };

    /// <summary>Maps a status without content: 204 when done, 401 for a deleted user, 404 otherwise.</summary>
    public static ActionResult ToActionResult(this ControllerBase controller, CalendarResultStatus status) => status switch
    {
        CalendarResultStatus.Ok => controller.NoContent(),
        CalendarResultStatus.UserNotFound => controller.Unauthorized(),
        _ => controller.NotFound(),
    };
}
