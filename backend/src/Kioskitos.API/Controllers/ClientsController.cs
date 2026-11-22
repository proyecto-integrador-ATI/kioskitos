using Kioskitos.API.Contracts;
using Kioskitos.Application.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController(RegisterClientService registerService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Register(
        RegisterClientRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        var command = new RegisterClientCommand(
            request.Name,
            request.Industry,
            request.Notes,
            request.Contacts
                .Select(c => new ClientContactData(c.Name, c.Email, c.Phone))
                .ToList());

        var result = await registerService.RegisterAsync(userId, command, ct);

        if (!result.Succeeded)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Could not register the client",
                Detail = "The client name and at least one complete contact person with a valid phone are required.",
                Instance = HttpContext.Request.Path
            };
            problem.Extensions["errorCode"] = "invalid_data";

            return BadRequest(problem);
        }

        var response = ClientResponse.From(result.Client!);
        return Created($"/api/clients/{response.Id}", response);
    }
}