using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/employees")]
[RequireRole(UserRole.Admin)]
public sealed class EmployeesController(EmployeeAdminService employeeAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.ListEmployeesAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateEmployeeProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.CreateEmployeeAsync(request, cancellationToken);
        return ToActionResult(result, createdAtRouteName: nameof(GetByIdAsync), routeValues: new { employeeId = result.Value?.EmployeeId });
    }

    [HttpGet("{employeeId:int}", Name = nameof(GetByIdAsync))]
    public async Task<IActionResult> GetByIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        var employees = await employeeAdminService.ListEmployeesAsync(cancellationToken);
        var employee = employees.Value?.FirstOrDefault(item => item.EmployeeId == employeeId);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPut("{employeeId:int}")]
    public async Task<IActionResult> UpdateAsync(
        int employeeId,
        [FromBody] UpdateEmployeeProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.UpdateEmployeeAsync(employeeId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{employeeId:int}/deactivate")]
    public async Task<IActionResult> DeactivateAsync(int employeeId, CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.DeactivateEmployeeAsync(employeeId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("assign-manager")]
    public async Task<IActionResult> AssignManagerAsync([FromBody] AssignManagerRequest request, CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.AssignManagerAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{employeeId:int}/skills")]
    public async Task<IActionResult> UpsertSkillAsync(
        int employeeId,
        [FromBody] UpsertEmployeeSkillRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.UpsertEmployeeSkillAsync(employeeId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{employeeId:int}/skills/{skillId:int}")]
    public async Task<IActionResult> RemoveSkillAsync(int employeeId, int skillId, CancellationToken cancellationToken)
    {
        var result = await employeeAdminService.RemoveEmployeeSkillAsync(employeeId, skillId, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(AdminResult<EmployeeSummaryDto> result, string? createdAtRouteName = null, object? routeValues = null)
    {
        if (result.Succeeded && createdAtRouteName is not null && result.Value is not null)
        {
            return CreatedAtRoute(createdAtRouteName, routeValues, result.Value);
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
