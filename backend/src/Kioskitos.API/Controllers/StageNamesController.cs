using Kioskitos.Application.StageNames;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/stage-names")]
public sealed class StageNamesController(StageNameService stageNameService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StageNameDto>>> List(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await stageNameService.ListAsync(userId, ct));
    }
}
