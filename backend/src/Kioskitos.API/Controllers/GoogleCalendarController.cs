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
                "The Google Calendar permission is required to create the Kioskitos calendar."),
            ConnectGoogleCalendarError.MissingRefreshToken => CalendarProblem(
                "missing_refresh_token",
                "Google did not return a refresh token. Remove Kioskitos from your Google account access and try again."),
            _ => CalendarProblem(
                "invalid_code",
                "The Google authorization code is invalid or expired. Please try again.")
        };
    }

    private BadRequestObjectResult CalendarProblem(string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Could not connect Google Calendar",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;

        return BadRequest(problem);
    }
}