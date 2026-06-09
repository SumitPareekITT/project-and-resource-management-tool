using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Ai;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/manager/ai")]
[RequireRole(UserRole.Manager)]
public sealed class ManagerAiController(AiAssistantService aiAssistantService) : ControllerBase
{
    [HttpPost("skill-match")]
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

    private bool TryGetManagerUserId(out int managerUserId, out IActionResult? errorResult)
    {
        managerUserId = 0;
        if (!Request.Headers.TryGetValue("X-User-Id", out var rawUserId) ||
            !int.TryParse(rawUserId.ToString(), out managerUserId))
        {
            errorResult = Unauthorized(new { Message = "Missing or invalid X-User-Id header." });
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
