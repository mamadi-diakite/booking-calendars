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
}
