using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Facts;

public sealed class ProjectRiskFactAssembler(
    ProjectRepository projectRepository,
    AllocationRepository allocationRepository,
    TimesheetRepository timesheetRepository,
    SystemConfigurationRepository systemConfigurationRepository)
{
    public async Task<ProjectRiskFacts?> AssembleOwnedProjectFactsAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.ManagerId != managerUserId)
        {
            return null;
        }

        return await AssembleFactsAsync(project, cancellationToken);
    }

    internal async Task<ProjectRiskFacts> AssembleFactsAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var previousWeekStart = GetPreviousWeekStart(today);
        var maxWeeklyHours = await ReadMaxWeeklyHoursAsync(cancellationToken);

        var activeAllocations = await allocationRepository.ListActiveByProjectAsync(project.Id, cancellationToken);
        var currentAllocations = activeAllocations
            .Where(allocation => allocation.FromDate <= today
                && (allocation.ToDate is null || allocation.ToDate >= today))
            .ToList();

        var weekAllocations = activeAllocations
            .Where(allocation => DateRangesOverlap(
                allocation.FromDate,
                allocation.ToDate,
                previousWeekStart,
                previousWeekStart.AddDays(6)))
            .ToList();

        var expectedHours = weekAllocations.Sum(allocation =>
            maxWeeklyHours * allocation.UtilizationPercentage / BusinessRules.FullAllocationPercent);

        var loggedHours = await timesheetRepository.SumProjectHoursForWeekAsync(
            project.Id,
            previousWeekStart,
            cancellationToken);

        return new ProjectRiskFacts(
            project.Id,
            project.Name,
            project.ClientName,
            project.Status,
            project.HealthStatus,
            project.StartDate,
            project.EndDate,
            project.TotalStoryPoints,
            project.CompletedStoryPoints,
            currentAllocations.Count,
            loggedHours,
            expectedHours,
            BuildMilestoneLines(project.Milestones, today),
            BuildAllocationLines(currentAllocations));
    }

    private async Task<int> ReadMaxWeeklyHoursAsync(CancellationToken cancellationToken)
    {
        var configuration = await systemConfigurationRepository.GetByKeyAsync("MaxWeeklyHours", cancellationToken);
        return int.TryParse(configuration?.Value, out var parsed) && parsed > 0
            ? parsed
            : BusinessRules.DefaultMaxWeeklyHours;
    }

    private static IReadOnlyList<string> BuildMilestoneLines(IEnumerable<Milestone> milestones, DateOnly today)
    {
        return milestones
            .OrderBy(milestone => milestone.DueDate)
            .Select(milestone =>
            {
                var overdueSuffix = milestone.DueDate < today && milestone.Status != MilestoneStatus.Completed
                    ? " (overdue)"
                    : string.Empty;
                return $"{milestone.Title}: due {milestone.DueDate:yyyy-MM-dd}, status {milestone.Status}, SP {milestone.CompletedStoryPoints}/{milestone.StoryPoints}{overdueSuffix}";
            })
            .ToList();
    }

    private static IReadOnlyList<string> BuildAllocationLines(IEnumerable<Allocation> allocations)
    {
        return allocations
            .Select(allocation =>
                $"{allocation.Employee.FullName}: {allocation.UtilizationPercentage:0.##}% from {allocation.FromDate:yyyy-MM-dd} to {(allocation.ToDate?.ToString("yyyy-MM-dd") ?? "open")}")
            .ToList();
    }

    internal static DateOnly GetPreviousWeekStart(DateOnly referenceDate)
    {
        var daysFromMonday = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return referenceDate.AddDays(-daysFromMonday - 7);
    }

    private static bool DateRangesOverlap(DateOnly from1, DateOnly? to1, DateOnly from2, DateOnly to2)
    {
        var end1 = to1 ?? DateOnly.MaxValue;
        return from1 <= to2 && from2 <= end1;
    }
}
