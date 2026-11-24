using Kioskitos.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/clients/{clientId:guid}/projects")]
public sealed class ClientProjectsController(ProjectQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> List(Guid clientId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        // Null when the client does not exist or belongs to another user.
        var projects = await queryService.ListByClientAsync(userId, clientId, ct);
        return projects is null ? NotFound() : Ok(projects);
    }
}
