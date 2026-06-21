using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Manager;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Server.Services.Timesheets;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.DTOs.Timesheet;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/manager")]
public sealed class ManagerController(
    AllocationManagerService allocationManagerService,
    ProjectHealthService projectHealthService,
    TimesheetService timesheetService,
    TimesheetComplianceService timesheetComplianceService) : ControllerBase
{
    [HttpGet("dashboard")]
    [RequirePermission(PermissionCodes.ManagerDashboardView)]
    public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await allocationManagerService.GetDashboardAsync(managerUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("projects")]
    [RequirePermission(PermissionCodes.ManagerProjectsList)]
    public async Task<IActionResult> ListProjectsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await allocationManagerService.ListOwnedProjectsAsync(managerUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("projects/health")]
    [RequirePermission(PermissionCodes.ManagerProjectsList)]
    public async Task<IActionResult> ListProjectHealthAsync(CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await projectHealthService.ListManagerProjectHealthAsync(managerUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("allocations")]
    [RequirePermission(PermissionCodes.ManagerAllocationsCreate)]
    public async Task<IActionResult> AllocateAsync([FromBody] CreateAllocationRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await allocationManagerService.AllocateAsync(managerUserId, request, cancellationToken);
        return ToAllocationActionResult(result, created: true);
    }

    [HttpPut("allocations/{allocationId:int}/end")]
    [RequirePermission(PermissionCodes.ManagerAllocationsEnd)]
    public async Task<IActionResult> EndAllocationAsync(int allocationId, CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await allocationManagerService.EndAllocationAsync(managerUserId, allocationId, cancellationToken);
        return ToAllocationActionResult(result);
    }

    [HttpGet("timesheets")]
    [RequirePermission(PermissionCodes.ManagerTimesheetsList)]
    public async Task<IActionResult> ListTeamTimesheetsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.ListManagerTeamTimesheetsAsync(managerUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("timesheets/missing")]
    [RequirePermission(PermissionCodes.ManagerTimesheetsList)]
    public async Task<IActionResult> GetMissingTimesheetsAsync([FromQuery] DateOnly? weekStartDate, CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.GetMissingTimesheetRemindersAsync(managerUserId, weekStartDate, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("timesheets/{timesheetId:int}")]
    [RequirePermission(PermissionCodes.ManagerTimesheetsView)]
    public async Task<IActionResult> GetTeamTimesheetAsync(int timesheetId, CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.GetManagerTeamTimesheetAsync(managerUserId, timesheetId, cancellationToken);
        return ToDetailActionResult(result);
    }

    [HttpGet("timesheets/frozen")]
    [RequirePermission(PermissionCodes.ManagerTimesheetsFrozenList)]
    public async Task<IActionResult> ListFrozenTimesheetEmployeesAsync(CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetComplianceService.ListFrozenTeamMembersAsync(managerUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("timesheets/compliance/{employeeUserId:int}/restore")]
    [RequirePermission(PermissionCodes.ManagerTimesheetsRestore)]
    public async Task<IActionResult> RestoreTimesheetAccessAsync(int employeeUserId, CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetComplianceService.RestoreTimesheetAccessAsync(managerUserId, employeeUserId, cancellationToken);
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

    private IActionResult ToAllocationActionResult(AdminResult<AllocationDetailDto> result, bool created = false)
    {
        if (result.Succeeded && created)
        {
            return Created(string.Empty, result.Value);
        }

        return ToActionResult(result);
    }

    private IActionResult ToDetailActionResult(AdminResult<TimesheetDetailDto> result)
    {
        return ToActionResult(result);
    }
}
