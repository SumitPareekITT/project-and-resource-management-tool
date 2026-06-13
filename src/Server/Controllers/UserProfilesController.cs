using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/user-profiles")]
public sealed class UserProfilesController(UserProfileAdminService userProfileAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.UserProfilesList)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.ListProfilesAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.UserProfilesCreate)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.CreateProfileAsync(request, cancellationToken);
        return ToActionResult(result, created: true);
    }

    [HttpPut("{profileId:int}")]
    [RequirePermission(PermissionCodes.UserProfilesUpdate)]
    public async Task<IActionResult> UpdateAsync(int profileId, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.UpdateProfileAsync(profileId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("assign-manager")]
    [RequirePermission(PermissionCodes.UserProfilesAssignManager)]
    public async Task<IActionResult> AssignManagerAsync([FromBody] AssignManagerRequest request, CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.AssignManagerAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{profileId:int}/deactivate")]
    [RequirePermission(PermissionCodes.UserProfilesDeactivate)]
    public async Task<IActionResult> DeactivateAsync(int profileId, CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.DeactivateProfileAsync(profileId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{profileId:int}/skills")]
    [RequirePermission(PermissionCodes.UserProfilesSkillsUpsert)]
    public async Task<IActionResult> UpsertSkillAsync(int profileId, [FromBody] UpsertUserSkillRequest request, CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.UpsertUserSkillAsync(profileId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("by-user/{userId:int}/skills")]
    [RequirePermission(PermissionCodes.UserProfilesSkillsUpsert)]
    public async Task<IActionResult> AddSkillByNameAsync(
        int userId,
        [FromBody] AddUserSkillByNameRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.AddOrUpdateUserSkillByNameAsync(userId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{profileId:int}/skills/{skillId:int}")]
    [RequirePermission(PermissionCodes.UserProfilesSkillsRemove)]
    public async Task<IActionResult> RemoveSkillAsync(int profileId, int skillId, CancellationToken cancellationToken)
    {
        var result = await userProfileAdminService.RemoveUserSkillAsync(profileId, skillId, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(AdminResult<UserProfileSummaryDto> result, bool created = false)
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
