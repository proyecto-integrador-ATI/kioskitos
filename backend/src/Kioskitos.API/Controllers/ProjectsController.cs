using Kioskitos.API.Contracts;
using Kioskitos.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(
    CreateProjectService createService,
    ProjectQueryService queryService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new CreateProjectCommand(
            request.ClientId!.Value,
            request.Name,
            request.Description,
            request.Objectives,
            request.Scope,
            request.HasStages,
            request.DueDate,
            request.Status,
            request.Tags ?? [],
            request.Stages ?? []);

        var result = await createService.CreateAsync(userId, command, ct);

        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Project!.Id }, result.Project);
        }

        return result.Error switch
        {
            CreateProjectError.ClientNotFound => NotFound(ProjectProblem(
                StatusCodes.Status404NotFound,
                "client_not_found",
                "The client does not exist.")),
            _ => BadRequest(ProjectProblem(
                StatusCodes.Status400BadRequest,
                "invalid_data",
                "The project data is invalid. Check the name, the text lengths, the status, the tags and the stages."))
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var project = await queryService.GetByIdAsync(userId, id, ct);
        return project is null ? NotFound() : Ok(project);
    }

    private ProblemDetails ProjectProblem(int status, string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = "Could not create the project",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;

        return problem;
    }
}
