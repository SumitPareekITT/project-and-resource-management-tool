using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class ProjectAdminService(
    ProjectRepository projectRepository,
    MilestoneRepository milestoneRepository,
    UserRepository userRepository,
    RbacRepository rbacRepository,
    AllocationRepository allocationRepository)
{
    public async Task<AdminResult<IReadOnlyList<ProjectSummaryDto>>> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.ListAsync(cancellationToken);
        var mapped = projects.Select(MapProject).ToList();
        return AdminResult<IReadOnlyList<ProjectSummaryDto>>.Success(mapped);
    }

    public async Task<AdminResult<ProjectSummaryDto>> GetProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.NotFound, "Project was not found.");
        }

        return AdminResult<ProjectSummaryDto>.Success(MapProject(project));
    }

    public async Task<AdminResult<ProjectSummaryDto>> CreateProjectAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateProjectFieldsAsync(
            request.Name,
            request.StartDate,
            request.EndDate,
            request.ManagerUserId,
            request.TotalStoryPoints,
            0,
            cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var project = new Project
        {
            Name = request.Name.Trim(),
            ClientName = request.ClientName.Trim(),
            Description = request.Description.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ManagerUserId = request.ManagerUserId,
            TotalStoryPoints = request.TotalStoryPoints,
            CompletedStoryPoints = 0,
            Status = ProjectStatus.Planned,
            HealthStatus = ProjectHealthStatus.OnTrack
        };

        await projectRepository.AddAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);

        var created = await projectRepository.GetByIdAsync(project.Id, cancellationToken);
        return AdminResult<ProjectSummaryDto>.Success(MapProject(created!));
    }

    public async Task<AdminResult<ProjectSummaryDto>> UpdateProjectAsync(
        int projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.NotFound, "Project was not found.");
        }

        var validationError = await ValidateProjectFieldsAsync(
            request.Name,
            request.StartDate,
            request.EndDate,
            request.ManagerUserId,
            request.TotalStoryPoints,
            request.CompletedStoryPoints,
            cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        project.Name = request.Name.Trim();
        project.ClientName = request.ClientName.Trim();
        project.Description = request.Description.Trim();
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.ManagerUserId = request.ManagerUserId;
        project.TotalStoryPoints = request.TotalStoryPoints;
        project.CompletedStoryPoints = request.CompletedStoryPoints;

        await projectRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<ProjectSummaryDto>.Success(MapProject(project));
    }

    public async Task<AdminResult<ProjectSummaryDto>> UpdateProjectStatusAsync(
        int projectId,
        UpdateProjectStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.NotFound, "Project was not found.");
        }

        project.Status = request.Status;
        await projectRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<ProjectSummaryDto>.Success(MapProject(project));
    }

    public async Task<AdminResult<MilestoneDto>> AddMilestoneAsync(
        int projectId,
        UpsertMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.NotFound, "Project was not found.");
        }

        var validationError = ValidateMilestoneFields(request.Title, request.StoryPoints, request.CompletedStoryPoints);
        if (validationError is not null)
        {
            return validationError;
        }

        var milestone = new Milestone
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DueDate = request.DueDate,
            Status = request.Status,
            StoryPoints = request.StoryPoints,
            CompletedStoryPoints = request.CompletedStoryPoints
        };

        await milestoneRepository.AddAsync(milestone, cancellationToken);
        await milestoneRepository.SaveChangesAsync(cancellationToken);
        SyncProjectStoryPoints(project);
        await projectRepository.SaveChangesAsync(cancellationToken);

        return AdminResult<MilestoneDto>.Success(MapMilestone(milestone));
    }

    public async Task<AdminResult<MilestoneDto>> UpdateMilestoneAsync(
        int projectId,
        int milestoneId,
        UpsertMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var milestone = await milestoneRepository.GetByIdAsync(milestoneId, cancellationToken);
        if (milestone is null || milestone.ProjectId != projectId)
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.NotFound, "Milestone was not found for this project.");
        }

        var validationError = ValidateMilestoneFields(request.Title, request.StoryPoints, request.CompletedStoryPoints);
        if (validationError is not null)
        {
            return validationError;
        }

        milestone.Title = request.Title.Trim();
        milestone.Description = request.Description.Trim();
        milestone.DueDate = request.DueDate;
        milestone.Status = request.Status;
        milestone.StoryPoints = request.StoryPoints;
        milestone.CompletedStoryPoints = request.CompletedStoryPoints;

        await milestoneRepository.SaveChangesAsync(cancellationToken);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is not null)
        {
            SyncProjectStoryPoints(project);
            await projectRepository.SaveChangesAsync(cancellationToken);
        }

        return AdminResult<MilestoneDto>.Success(MapMilestone(milestone));
    }

    public async Task<AdminResult<MilestoneDto>> UpdateMilestoneStatusAsync(
        int projectId,
        int milestoneId,
        UpdateMilestoneStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var milestone = await milestoneRepository.GetByIdAsync(milestoneId, cancellationToken);
        if (milestone is null || milestone.ProjectId != projectId)
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.NotFound, "Milestone was not found for this project.");
        }

        milestone.Status = request.Status;
        await milestoneRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<MilestoneDto>.Success(MapMilestone(milestone));
    }

    public async Task<AdminResult<IReadOnlyList<AllocationMatrixRowDto>>> GetAllocationMatrixAsync(
        CancellationToken cancellationToken = default)
    {
        var allocations = await allocationRepository.ListAllActiveAsync(cancellationToken);
        var rows = allocations.Select(allocation => new AllocationMatrixRowDto(
            allocation.Id,
            allocation.UserId,
            allocation.User.Profile!.FullName,
            allocation.ProjectId,
            allocation.Project.Name,
            allocation.Project.ManagerUser.Profile!.FullName,
            allocation.UtilizationPercentage,
            allocation.FromDate,
            allocation.ToDate,
            allocation.Status.ToString())).ToList();

        return AdminResult<IReadOnlyList<AllocationMatrixRowDto>>.Success(rows);
    }

    private async Task<AdminResult<ProjectSummaryDto>?> ValidateProjectFieldsAsync(
        string name,
        DateOnly startDate,
        DateOnly endDate,
        int managerUserId,
        int totalStoryPoints,
        int completedStoryPoints,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "Project name is required.");
        }

        if (endDate < startDate)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "End date cannot be before start date.");
        }

        if (totalStoryPoints < 0)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "Total story points cannot be negative.");
        }

        if (completedStoryPoints < 0)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "Completed story points cannot be negative.");
        }

        if (completedStoryPoints > totalStoryPoints)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "Completed story points cannot exceed total story points.");
        }

        var manager = await userRepository.GetByIdAsync(managerUserId, cancellationToken);
        if (manager is null || !manager.IsActive)
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "Manager must be an active user.");
        }

        var managerRoles = await rbacRepository.GetRoleNamesForUserAsync(managerUserId, cancellationToken);
        if (!managerRoles.Contains(nameof(UserRole.Manager), StringComparer.OrdinalIgnoreCase))
        {
            return AdminResult<ProjectSummaryDto>.Fail(AdminResultCode.ValidationError, "Manager must have Manager role.");
        }

        return null;
    }

    private static AdminResult<MilestoneDto>? ValidateMilestoneFields(string title, int storyPoints, int completedStoryPoints)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.ValidationError, "Milestone title is required.");
        }

        if (storyPoints < 0)
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.ValidationError, "Milestone story points cannot be negative.");
        }

        if (completedStoryPoints < 0)
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.ValidationError, "Completed story points cannot be negative.");
        }

        if (completedStoryPoints > storyPoints)
        {
            return AdminResult<MilestoneDto>.Fail(AdminResultCode.ValidationError, "Completed story points cannot exceed milestone story points.");
        }

        return null;
    }

    private static void SyncProjectStoryPoints(Project project)
    {
        project.TotalStoryPoints = project.Milestones.Sum(milestone => milestone.StoryPoints);
        project.CompletedStoryPoints = project.Milestones.Sum(milestone => milestone.CompletedStoryPoints);
    }

    private static ProjectSummaryDto MapProject(Project project)
    {
        var milestones = project.Milestones
            .OrderBy(milestone => milestone.DueDate)
            .Select(MapMilestone)
            .ToList();

        return new ProjectSummaryDto(
            project.Id,
            project.Name,
            project.ClientName,
            project.Status,
            project.HealthStatus,
            project.ManagerUserId,
            project.ManagerUser.Profile!.FullName,
            project.StartDate,
            project.EndDate,
            project.TotalStoryPoints,
            project.CompletedStoryPoints,
            $"{project.CompletedStoryPoints}/{project.TotalStoryPoints}",
            milestones);
    }

    private static MilestoneDto MapMilestone(Milestone milestone)
    {
        return new MilestoneDto(
            milestone.Id,
            milestone.ProjectId,
            milestone.Title,
            milestone.Description,
            milestone.DueDate,
            milestone.Status,
            milestone.StoryPoints,
            milestone.CompletedStoryPoints);
    }
}
