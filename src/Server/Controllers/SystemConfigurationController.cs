using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/system-configuration")]
public sealed class SystemConfigurationController(SystemConfigurationAdminService systemConfigurationAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.SystemConfigurationRead)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var result = await systemConfigurationAdminService.GetSettingsAsync(cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{key}")]
    [RequirePermission(PermissionCodes.SystemConfigurationUpdate)]
    public async Task<IActionResult> UpdateSettingAsync(
        string key,
        [FromBody] UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await systemConfigurationAdminService.UpdateSettingAsync(key, request, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(AdminResult<SystemSettingsDto> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.Code switch
        {
            AdminResultCode.NotFound => NotFound(new { result.Message }),
            AdminResultCode.ValidationError => BadRequest(new { result.Message }),
            _ => BadRequest(new { result.Message })
        };
    }
}
