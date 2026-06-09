using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Manager;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Server.Services.Timesheets;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.DTOs.Timesheet;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/manager")]
[RequireRole(UserRole.Manager)]
public sealed class ManagerController(
    AllocationManagerService allocationManagerService,
    ProjectHealthService projectHealthService,
    TimesheetService timesheetService) : ControllerBase
{
    [HttpGet("dashboard")]
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
    public async Task<IActionResult> GetTeamTimesheetAsync(int timesheetId, CancellationToken cancellationToken)
    {
        if (!TryGetManagerUserId(out var managerUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.GetManagerTeamTimesheetAsync(managerUserId, timesheetId, cancellationToken);
        return ToDetailActionResult(result);
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
