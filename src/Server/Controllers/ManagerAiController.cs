using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Ai;
using ProjectResourceManagement.Shared.DTOs.Ai;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/manager/ai")]
public sealed class ManagerAiController(AiAssistantService aiAssistantService) : ControllerBase
{
    [HttpPost("skill-match")]
    [RequirePermission(PermissionCodes.ManagerAiSkillMatch)]
    public async Task<IActionResult> MatchSkillsAsync(
        [FromBody] AiSkillMatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await aiAssistantService.MatchSkillsAsync(managerUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("project-risk-summary")]
    [RequirePermission(PermissionCodes.ManagerAiProjectRisk)]
    public async Task<IActionResult> SummarizeProjectRiskAsync(
        [FromBody] AiProjectRiskSummaryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await aiAssistantService.SummarizeProjectRiskAsync(managerUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("team-match")]
    [RequirePermission(PermissionCodes.ManagerAiTeamMatch)]
    public async Task<IActionResult> MatchOrganizationTeamAsync(
        [FromBody] AiTeamMatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await aiAssistantService.MatchOrganizationTeamAsync(managerUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    private bool TryGetManagerUserId(out int managerUserId, out IActionResult? errorResult)
    {
        managerUserId = 0;
        if (!HttpContext.TryGetAuthenticatedUserId(out managerUserId, out var errorMessage))
        {
            errorResult = Unauthorized(new { Message = errorMessage });
            return false;
        }

        errorResult = null;
        return true;
    }

    private IActionResult ToActionResult<T>(AdminResult<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.Code switch
        {
            AdminResultCode.NotFound => NotFound(new { result.Message }),
            AdminResultCode.ValidationError => BadRequest(new { result.Message }),
            AdminResultCode.Conflict => Conflict(new { result.Message }),
            _ => BadRequest(new { result.Message })
        };
    }
}
