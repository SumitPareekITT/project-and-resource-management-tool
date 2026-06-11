using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Manager;

public sealed class AllocationManagerService(
    UserProfileRepository userProfileRepository,
    ProjectRepository projectRepository,
    AllocationRepository allocationRepository,
    UtilizationComputationService utilizationComputationService)
{
    public async Task<AdminResult<IReadOnlyList<ResourceDashboardRowDto>>> GetDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await userProfileRepository.ListByManagerUserIdAsync(managerUserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = new List<ResourceDashboardRowDto>();

        foreach (var profile in team.Where(member => member.IsActive))
        {
            var activeAllocations = await allocationRepository.ListActiveByUserIdAsync(profile.UserId, cancellationToken);
            var currentAllocations = activeAllocations
                .Where(allocation => IsActiveOnDate(allocation, today))
                .ToList();

            var utilization = currentAllocations.Sum(allocation => allocation.UtilizationPercentage);
            var summary = currentAllocations.Count == 0
                ? "-"
                : string.Join(", ", currentAllocations.Select(allocation => $"{allocation.Project.Name} ({allocation.UtilizationPercentage:0.##}%)"));

            rows.Add(new ResourceDashboardRowDto(
                profile.UserId,
                profile.FullName,
                profile.Department,
                profile.Designation,
                utilization,
                MapDashboardCategory(utilization),
                summary));
        }

        return AdminResult<IReadOnlyList<ResourceDashboardRowDto>>.Success(
            rows.OrderBy(row => row.FullName).ToList());
    }

    public async Task<AdminResult<IReadOnlyList<ManagerProjectOptionDto>>> ListOwnedProjectsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.ListByManagerUserIdAsync(managerUserId, cancellationToken);
        var mapped = projects
            .Where(project => project.Status is ProjectStatus.Planned or ProjectStatus.Active)
            .Select(project => new ManagerProjectOptionDto(
                project.Id,
                project.Name,
                project.ClientName,
                project.Status,
                project.HealthStatus,
                ProjectHealthService.FormatStoryPointProgress(project),
                project.StartDate,
                project.EndDate))
            .ToList();

        return AdminResult<IReadOnlyList<ManagerProjectOptionDto>>.Success(mapped);
    }

    public async Task<AdminResult<AllocationDetailDto>> AllocateAsync(
        int managerUserId,
        CreateAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UtilizationPercentage is < BusinessRules.MinimumAllocationPercent or > BusinessRules.FullAllocationPercent)
        {
            return AdminResult<AllocationDetailDto>.Fail(
                AdminResultCode.ValidationError,
                $"Utilization must be between {BusinessRules.MinimumAllocationPercent}% and {BusinessRules.FullAllocationPercent}%.");
        }

        if (request.ToDate is not null && request.ToDate < request.FromDate)
        {
            return AdminResult<AllocationDetailDto>.Fail(AdminResultCode.ValidationError, "To date cannot be before from date.");
        }

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return AdminResult<AllocationDetailDto>.Fail(AdminResultCode.NotFound, "Project was not found.");
        }

        if (project.ManagerUserId != managerUserId)
        {
            return AdminResult<AllocationDetailDto>.Fail(AdminResultCode.ValidationError, "You can allocate resources only to projects you own.");
        }

        var profile = await userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null || !profile.IsActive)
        {
            return AdminResult<AllocationDetailDto>.Fail(AdminResultCode.NotFound, "User profile was not found or is inactive.");
        }

        if (profile.ManagerUserId != managerUserId)
        {
            return AdminResult<AllocationDetailDto>.Fail(
                AdminResultCode.ValidationError,
                "You can allocate only team members assigned to your direct team.");
        }

        var activeAllocations = await allocationRepository.ListActiveByUserIdAsync(profile.UserId, cancellationToken);
        var overlappingUtilization = activeAllocations
            .Where(allocation => DateRangesOverlap(allocation.FromDate, allocation.ToDate, request.FromDate, request.ToDate))
            .Sum(allocation => allocation.UtilizationPercentage);

        if (overlappingUtilization + request.UtilizationPercentage > BusinessRules.FullAllocationPercent)
        {
            return AdminResult<AllocationDetailDto>.Fail(
                AdminResultCode.ValidationError,
                $"Allocation would exceed {BusinessRules.FullAllocationPercent}% capacity for the selected period.");
        }

        var allocation = new Allocation
        {
            UserId = profile.UserId,
            ProjectId = project.Id,
            CreatedByUserId = managerUserId,
            UtilizationPercentage = request.UtilizationPercentage,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Status = AllocationStatus.Active
        };

        await allocationRepository.AddAsync(allocation, cancellationToken);
        await allocationRepository.SaveChangesAsync(cancellationToken);
        await utilizationComputationService.SyncUserProfileAsync(profile, cancellationToken);
        await userProfileRepository.SaveChangesAsync(cancellationToken);

        var created = await allocationRepository.GetByIdAsync(allocation.Id, cancellationToken);
        return AdminResult<AllocationDetailDto>.Success(MapAllocation(created!));
    }

    public async Task<AdminResult<AllocationDetailDto>> EndAllocationAsync(
        int managerUserId,
        int allocationId,
        CancellationToken cancellationToken = default)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId, cancellationToken);
        if (allocation is null)
        {
            return AdminResult<AllocationDetailDto>.Fail(AdminResultCode.NotFound, "Allocation was not found.");
        }

        if (allocation.Project.ManagerUserId != managerUserId)
        {
            return AdminResult<AllocationDetailDto>.Fail(
                AdminResultCode.ValidationError,
                "You can end allocations only on projects you own.");
        }

        if (allocation.Status == AllocationStatus.Ended)
        {
            return AdminResult<AllocationDetailDto>.Success(MapAllocation(allocation), "Allocation is already ended.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        allocation.Status = AllocationStatus.Ended;
        if (allocation.ToDate is null || allocation.ToDate > today)
        {
            allocation.ToDate = today;
        }

        if (allocation.User.Profile is not null)
        {
            await utilizationComputationService.SyncUserProfileAsync(allocation.User.Profile, cancellationToken);
        }

        await allocationRepository.SaveChangesAsync(cancellationToken);
        await userProfileRepository.SaveChangesAsync(cancellationToken);

        return AdminResult<AllocationDetailDto>.Success(MapAllocation(allocation), "Allocation ended successfully.");
    }

    private static bool IsActiveOnDate(Allocation allocation, DateOnly date)
    {
        return allocation.Status == AllocationStatus.Active
            && allocation.FromDate <= date
            && (allocation.ToDate is null || allocation.ToDate >= date);
    }

    private static bool DateRangesOverlap(DateOnly from1, DateOnly? to1, DateOnly from2, DateOnly? to2)
    {
        var end1 = to1 ?? DateOnly.MaxValue;
        var end2 = to2 ?? DateOnly.MaxValue;
        return from1 <= end2 && from2 <= end1;
    }

    private static ResourceDashboardCategory MapDashboardCategory(decimal utilization)
    {
        if (utilization == 0)
        {
            return ResourceDashboardCategory.Bench;
        }

        if (utilization > BusinessRules.FullAllocationPercent)
        {
            return ResourceDashboardCategory.Overallocated;
        }

        if (utilization == BusinessRules.FullAllocationPercent)
        {
            return ResourceDashboardCategory.Allocated;
        }

        return ResourceDashboardCategory.PartiallyAllocated;
    }

    private static AllocationDetailDto MapAllocation(Allocation allocation)
    {
        var userName = allocation.User.Profile?.FullName ?? allocation.User.Username;
        return new AllocationDetailDto(
            allocation.Id,
            allocation.UserId,
            userName,
            allocation.ProjectId,
            allocation.Project.Name,
            allocation.UtilizationPercentage,
            allocation.FromDate,
            allocation.ToDate,
            allocation.Status);
    }
}