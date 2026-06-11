using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(SkillAdminService skillAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.SkillsList)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await skillAdminService.ListSkillsAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.SkillsCreate)]
    public async Task<IActionResult> CreateAsync([FromBody] UpsertSkillRequest request, CancellationToken cancellationToken)
    {
        var result = await skillAdminService.CreateSkillAsync(request, cancellationToken);
        return ToActionResult(result, created: true);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.SkillsUpdate)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpsertSkillRequest request, CancellationToken cancellationToken)
    {
        var result = await skillAdminService.UpdateSkillAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:int}/deactivate")]
    [RequirePermission(PermissionCodes.SkillsDeactivate)]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var result = await skillAdminService.DeactivateSkillAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(AdminResult<SkillDto> result, bool created = false)
    {
        if (result.Succeeded && created)
        {
            return Created(string.Empty, result.Value);
        }

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
