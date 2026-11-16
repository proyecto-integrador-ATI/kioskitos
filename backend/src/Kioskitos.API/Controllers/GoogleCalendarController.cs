using Kioskitos.API.Contracts;
using Kioskitos.Application.GoogleCalendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/google-calendar")]
public sealed class GoogleCalendarController(ConnectGoogleCalendarService connectService) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect(ConnectGoogleCalendarRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        var result = await connectService.ConnectAsync(userId, request.Code, ct);

        if (result.Succeeded)
        {
            return Ok(new ConnectGoogleCalendarResponse(result.CalendarId!, true));
        }

        return result.Error switch
        {
            ConnectGoogleCalendarError.MissingCalendarScope => CalendarProblem(
                "missing_calendar_scope",
                "Para crear el calendario Kioskitos tenés que aceptar el permiso de Google Calendar."),
            ConnectGoogleCalendarError.MissingRefreshToken => CalendarProblem(
                "missing_refresh_token",
                "Google no devolvió el permiso de acceso. Quitá Kioskitos de los accesos de tu cuenta de Google y volvé a intentarlo."),
            _ => CalendarProblem(
                "invalid_code",
                "No pudimos validar la autorización de Google. Volvé a intentarlo.")
        };
    }

    private BadRequestObjectResult CalendarProblem(string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "No se pudo conectar Google Calendar",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;

        return BadRequest(problem);
    }
}