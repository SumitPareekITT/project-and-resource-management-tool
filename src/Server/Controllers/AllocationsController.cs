using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/allocations")]
[RequireRole(UserRole.Admin)]
public sealed class AllocationsController(ProjectAdminService projectAdminService) : ControllerBase
{
    [HttpGet("matrix")]
    public async Task<IActionResult> GetMatrixAsync(CancellationToken cancellationToken)
    {
        var result = await projectAdminService.GetAllocationMatrixAsync(cancellationToken);
        return Ok(result.Value);
    }
}
