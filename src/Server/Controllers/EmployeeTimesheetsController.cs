using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Timesheets;
using ProjectResourceManagement.Shared.DTOs.Timesheet;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/employee")]
[RequireRole(UserRole.Employee)]
public sealed class EmployeeTimesheetsController(TimesheetService timesheetService) : ControllerBase
{
    [HttpGet("timesheets/active-projects")]
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
    public async Task<IActionResult> ListAllocationsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeUserId(out var employeeUserId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await timesheetService.ListEmployeeAllocationsAsync(employeeUserId, cancellationToken);
        return ToActionResult(result);
    }

    private bool TryGetEmployeeUserId(out int employeeUserId, out IActionResult? errorResult)
    {
        employeeUserId = 0;
        if (!Request.Headers.TryGetValue("X-User-Id", out var rawUserId) ||
            !int.TryParse(rawUserId.ToString(), out employeeUserId))
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

    private IActionResult ToDetailActionResult(AdminResult<TimesheetDetailDto> result, bool created = false)
    {
        if (result.Succeeded && created)
        {
            return Created(string.Empty, result.Value);
        }

        return ToActionResult(result);
    }
}
