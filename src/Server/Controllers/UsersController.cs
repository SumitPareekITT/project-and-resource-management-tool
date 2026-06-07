using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/users")]
[RequireRole(UserRole.Admin)]
public sealed class UsersController(UserAdminService userAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await userAdminService.ListUsersAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await userAdminService.CreateUserAsync(request, cancellationToken);
        return ToActionResult(result, created: true);
    }

    [HttpPut("{userId:int}/reset-password")]
    public async Task<IActionResult> ResetPasswordAsync(
        int userId,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdminService.ResetPasswordAsync(userId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{userId:int}/deactivate")]
    public async Task<IActionResult> DeactivateAsync(int userId, CancellationToken cancellationToken)
    {
        var result = await userAdminService.DeactivateUserAsync(userId, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(AdminResult<UserSummaryDto> result, bool created = false)
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
