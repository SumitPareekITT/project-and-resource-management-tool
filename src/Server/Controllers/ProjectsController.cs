using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(ProjectAdminService projectAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.ProjectsList)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await projectAdminService.ListProjectsAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{projectId:int}", Name = nameof(GetProjectByIdAsync))]
    [RequirePermission(PermissionCodes.ProjectsList)]
    public async Task<IActionResult> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken)
    {
        var result = await projectAdminService.GetProjectAsync(projectId, cancellationToken);
        return ToProjectActionResult(result);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.ProjectsCreate)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await projectAdminService.CreateProjectAsync(request, cancellationToken);
        return ToProjectActionResult(result, createdAtRouteName: nameof(GetProjectByIdAsync), routeValues: new { projectId = result.Value?.ProjectId });
    }

    [HttpPut("{projectId:int}")]
    [RequirePermission(PermissionCodes.ProjectsUpdate)]
    public async Task<IActionResult> UpdateAsync(
        int projectId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectAdminService.UpdateProjectAsync(projectId, request, cancellationToken);
        return ToProjectActionResult(result);
    }

    [HttpPut("{projectId:int}/status")]
    [RequirePermission(PermissionCodes.ProjectsUpdateStatus)]
    public async Task<IActionResult> UpdateStatusAsync(
        int projectId,
        [FromBody] UpdateProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectAdminService.UpdateProjectStatusAsync(projectId, request, cancellationToken);
        return ToProjectActionResult(result);
    }

    [HttpPost("{projectId:int}/milestones")]
    [RequirePermission(PermissionCodes.ProjectsMilestonesManage)]
    public async Task<IActionResult> AddMilestoneAsync(
        int projectId,
        [FromBody] UpsertMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectAdminService.AddMilestoneAsync(projectId, request, cancellationToken);
        return ToMilestoneActionResult(result, created: true);
    }

    [HttpPut("{projectId:int}/milestones/{milestoneId:int}")]
    [RequirePermission(PermissionCodes.ProjectsMilestonesManage)]
    public async Task<IActionResult> UpdateMilestoneAsync(
        int projectId,
        int milestoneId,
        [FromBody] UpsertMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectAdminService.UpdateMilestoneAsync(projectId, milestoneId, request, cancellationToken);
        return ToMilestoneActionResult(result);
    }

    [HttpPut("{projectId:int}/milestones/{milestoneId:int}/status")]
    [RequirePermission(PermissionCodes.ProjectsMilestonesManage)]
    public async Task<IActionResult> UpdateMilestoneStatusAsync(
        int projectId,
        int milestoneId,
        [FromBody] UpdateMilestoneStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectAdminService.UpdateMilestoneStatusAsync(projectId, milestoneId, request, cancellationToken);
        return ToMilestoneActionResult(result);
    }

    private IActionResult ToProjectActionResult(
        AdminResult<ProjectSummaryDto> result,
        string? createdAtRouteName = null,
        object? routeValues = null)
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

    private IActionResult ToMilestoneActionResult(AdminResult<MilestoneDto> result, bool created = false)
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
