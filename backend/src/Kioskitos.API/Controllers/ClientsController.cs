using Kioskitos.API.Contracts;
using Kioskitos.Application.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController(
    RegisterClientService registerService,
    ClientQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientSummaryDto>>> List(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await queryService.ListAsync(userId, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await queryService.GetByIdAsync(userId, id, ct);

        if (result.Succeeded)
        {
            return Ok(result.Client);
        }

        // Another user's client is reported as missing, so its existence is not revealed.
        return NotFound(ClientProblem(
            StatusCodes.Status404NotFound,
            "Could not get the client",
            "client_not_found",
            "The client does not exist."));
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Register(
        RegisterClientRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
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
            return BadRequest(ClientProblem(
                StatusCodes.Status400BadRequest,
                "Could not register the client",
                "invalid_data",
                "The client name and at least one complete contact person with a valid phone are required."));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Client!.Id }, result.Client);
    }

    private ProblemDetails ClientProblem(int status, string title, string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;

        return problem;
    }
}
