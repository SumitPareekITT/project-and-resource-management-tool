using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/allocations")]
[RequirePermission(PermissionCodes.AllocationsMatrix)]
public sealed class AllocationsController(ProjectAdminService projectAdminService) : ControllerBase
{
    [HttpGet("matrix")]
    public async Task<IActionResult> GetMatrixAsync(CancellationToken cancellationToken)
    {
        var result = await projectAdminService.GetAllocationMatrixAsync(cancellationToken);
        return Ok(result.Value);
    }
}
