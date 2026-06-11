using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Timesheets;
using ProjectResourceManagement.Shared.DTOs.Timesheet;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/employee")]
public sealed class EmployeeTimesheetsController(TimesheetService timesheetService) : ControllerBase
{
    [HttpGet("timesheets/active-projects")]
    [RequirePermission(PermissionCodes.EmployeeTimesheetsSubmit)]
    public async Task<IActionResult> GetActiveProjectsAsync([FromQuery] DateOnly weekStartDate, CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.GetActiveProjectsForWeekAsync(employeeUserId, weekStartDate, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("timesheets")]
    [RequirePermission(PermissionCodes.EmployeeTimesheetsSubmit)]
    public async Task<IActionResult> SubmitAsync([FromBody] SubmitTimesheetRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.SubmitAsync(employeeUserId, request, cancellationToken);
        return ToDetailActionResult(result, created: true);
    }

    [HttpGet("timesheets")]
    [RequirePermission(PermissionCodes.EmployeeTimesheetsHistory)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.ListEmployeeHistoryAsync(employeeUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("timesheets/{weekStartDate}")]
    [RequirePermission(PermissionCodes.EmployeeTimesheetsView)]
    public async Task<IActionResult> GetByWeekAsync(DateOnly weekStartDate, CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.GetEmployeeTimesheetAsync(employeeUserId, weekStartDate, cancellationToken);
        return ToDetailActionResult(result);
    }

    [HttpGet("allocations")]
    [RequirePermission(PermissionCodes.EmployeeAllocationsView)]
    public async Task<IActionResult> ListAllocationsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.ListEmployeeAllocationsAsync(employeeUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("activity-tags")]
    [RequirePermission(PermissionCodes.EmployeeActivityTagsList)]
    public async Task<IActionResult> ListActivityTagsAsync(CancellationToken cancellationToken)
    {
        var result = await timesheetService.ListActivityTagsAsync(cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("timesheets/missing-reminder")]
    [RequirePermission(PermissionCodes.EmployeeMissingReminder)]
    public async Task<IActionResult> GetMissingTimesheetReminderAsync(CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.GetEmployeeMissingTimesheetReminderAsync(employeeUserId, cancellationToken);
        return ToActionResult(result);
    }

    private bool TryGetEmployeeUserId(out int employeeUserId, out IActionResult? errorResult)
    {
        employeeUserId = 0;
        if (!HttpContext.TryGetAuthenticatedUserId(out employeeUserId, out var errorMessage))
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

    private IActionResult ToDetailActionResult(AdminResult<TimesheetDetailDto> result, bool created = false)
    {
        if (result.Succeeded && created)
        {
            return Created(string.Empty, result.Value);
        }

        return ToActionResult(result);
    }
}
