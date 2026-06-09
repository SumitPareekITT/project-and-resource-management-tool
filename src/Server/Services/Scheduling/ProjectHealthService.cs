using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Scheduling;

public sealed class ProjectHealthService(
    ProjectRepository projectRepository,
    AllocationRepository allocationRepository,
    TimesheetRepository timesheetRepository,
    SystemConfigurationRepository systemConfigurationRepository)
{
    public async Task<int> EvaluateAndPersistAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.ListForHealthEvaluationAsync(cancellationToken);
        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var previousWeekStart = GetPreviousWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        foreach (var project in projects)
        {
            var evaluation = await EvaluateProjectAsync(project, maxWeeklyHours, previousWeekStart, cancellationToken);
            project.HealthStatus = evaluation.HealthStatus;
        }

        await projectRepository.SaveChangesAsync(cancellationToken);
        return projects.Count;
    }

    public async Task<AdminResult<IReadOnlyList<ManagerProjectHealthDto>>> ListManagerProjectHealthAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.ListByManagerIdAsync(managerUserId, cancellationToken);
        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var previousWeekStart = GetPreviousWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var mapped = new List<ManagerProjectHealthDto>();

        foreach (var project in projects.Where(item => item.Status is ProjectStatus.Planned or ProjectStatus.Active))
        {
            var loaded = await projectRepository.GetByIdAsync(project.Id, cancellationToken);
            if (loaded is null)
            {
                continue;
            }

            var evaluation = await EvaluateProjectAsync(loaded, maxWeeklyHours, previousWeekStart, cancellationToken);
            mapped.Add(MapHealthDto(loaded, evaluation));
        }

        return AdminResult<IReadOnlyList<ManagerProjectHealthDto>>.Success(
            mapped.OrderBy(item => item.Name).ToList());
    }

    internal async Task<ProjectHealthEvaluation> EvaluateProjectAsync(
        Project project,
        int maxWeeklyHours,
        DateOnly previousWeekStart,
        CancellationToken cancellationToken = default)
    {
        if (project.Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
        {
            return new ProjectHealthEvaluation(
                ProjectHealthStatus.OnTrack,
                ["No risk signals detected."],
                0,
                0,
                0);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var signals = new List<(ProjectHealthStatus Severity, string Message)>();
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

        EvaluateMilestones(project, today, signals);
        EvaluateScheduleAndStoryPoints(project, today, signals);
        EvaluateAllocations(project, signals, currentAllocations);
        await EvaluateRecentTimesheetsAsync(
            project,
            weekAllocations,
            expectedHours,
            loggedHours,
            previousWeekStart,
            signals,
            cancellationToken);

        if (signals.Count == 0)
        {
            return new ProjectHealthEvaluation(
                ProjectHealthStatus.OnTrack,
                ["No risk signals detected."],
                currentAllocations.Count,
                loggedHours,
                expectedHours);
        }

        var healthStatus = signals.Max(signal => signal.Severity);
        var messages = signals
            .OrderByDescending(signal => signal.Severity)
            .Select(signal => signal.Message)
            .Distinct()
            .ToList();

        return new ProjectHealthEvaluation(
            healthStatus,
            messages,
            currentAllocations.Count,
            loggedHours,
            expectedHours);
    }

    private static void EvaluateMilestones(
        Project project,
        DateOnly today,
        ICollection<(ProjectHealthStatus Severity, string Message)> signals)
    {
        foreach (var milestone in project.Milestones.Where(item => item.Status != MilestoneStatus.Completed))
        {
            if (milestone.DueDate < today)
            {
                signals.Add((
                    ProjectHealthStatus.AtRisk,
                    $"Milestone '{milestone.Title}' is overdue (due {milestone.DueDate:yyyy-MM-dd})."));
                continue;
            }

            var daysUntilDue = milestone.DueDate.DayNumber - today.DayNumber;
            if (daysUntilDue <= BusinessRules.MilestoneDueSoonDays)
            {
                signals.Add((
                    ProjectHealthStatus.Attention,
                    $"Milestone '{milestone.Title}' is due within {BusinessRules.MilestoneDueSoonDays} days."));
            }
        }
    }

    private static void EvaluateScheduleAndStoryPoints(
        Project project,
        DateOnly today,
        ICollection<(ProjectHealthStatus Severity, string Message)> signals)
    {
        if (project.Status == ProjectStatus.Active
            && project.EndDate < today
            && project.CompletedStoryPoints < project.TotalStoryPoints)
        {
            signals.Add((
                ProjectHealthStatus.AtRisk,
                "Project end date has passed with incomplete story points."));
        }

        if (project.TotalStoryPoints <= 0 || project.Status != ProjectStatus.Active)
        {
            return;
        }

        var totalDays = project.EndDate.DayNumber - project.StartDate.DayNumber;
        if (totalDays <= 0)
        {
            return;
        }

        var elapsedDays = Math.Clamp(today.DayNumber - project.StartDate.DayNumber, 0, totalDays);
        var expectedProgress = (decimal)elapsedDays / totalDays;
        var actualProgress = (decimal)project.CompletedStoryPoints / project.TotalStoryPoints;
        var behindPercent = Math.Round((expectedProgress - actualProgress) * 100m, 2);

        if (behindPercent >= BusinessRules.StoryPointBehindAtRiskPercent)
        {
            signals.Add((
                ProjectHealthStatus.AtRisk,
                $"Story-point progress is {behindPercent:0.##}% behind schedule."));
        }
        else if (behindPercent >= BusinessRules.StoryPointBehindAttentionPercent)
        {
            signals.Add((
                ProjectHealthStatus.Attention,
                $"Story-point progress is {behindPercent:0.##}% behind schedule."));
        }
    }

    private static void EvaluateAllocations(
        Project project,
        ICollection<(ProjectHealthStatus Severity, string Message)> signals,
        IReadOnlyList<Allocation> currentAllocations)
    {
        if (project.Status == ProjectStatus.Active && currentAllocations.Count == 0)
        {
            signals.Add((
                ProjectHealthStatus.AtRisk,
                "Active project has no current team allocations."));
        }
    }

    private async Task EvaluateRecentTimesheetsAsync(
        Project project,
        IReadOnlyList<Allocation> weekAllocations,
        decimal expectedHours,
        decimal loggedHours,
        DateOnly previousWeekStart,
        ICollection<(ProjectHealthStatus Severity, string Message)> signals,
        CancellationToken cancellationToken)
    {
        if (project.Status != ProjectStatus.Active || weekAllocations.Count == 0)
        {
            return;
        }

        var submittedEmployeeIds = await timesheetRepository.ListSubmittedEmployeeIdsForProjectWeekAsync(
            project.Id,
            previousWeekStart,
            cancellationToken);

        var missingEmployees = weekAllocations
            .Where(allocation => !submittedEmployeeIds.Contains(allocation.EmployeeId))
            .Select(allocation => allocation.Employee.FullName)
            .Distinct()
            .ToList();

        if (missingEmployees.Count > 0)
        {
            signals.Add((
                ProjectHealthStatus.Attention,
                $"Missing previous-week timesheets from: {string.Join(", ", missingEmployees)}."));
        }

        if (expectedHours <= 0)
        {
            return;
        }

        var fulfillmentPercent = Math.Round(loggedHours / expectedHours * 100m, 2);
        if (fulfillmentPercent < BusinessRules.TimesheetShortfallAtRiskPercent)
        {
            signals.Add((
                ProjectHealthStatus.AtRisk,
                $"Previous-week logged hours are {fulfillmentPercent:0.##}% of expected allocation ({loggedHours:0.##}/{expectedHours:0.##}h)."));
        }
        else if (fulfillmentPercent < BusinessRules.TimesheetShortfallAttentionPercent)
        {
            signals.Add((
                ProjectHealthStatus.Attention,
                $"Previous-week logged hours are {fulfillmentPercent:0.##}% of expected allocation ({loggedHours:0.##}/{expectedHours:0.##}h)."));
        }
    }

    private async Task<int> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken)
    {
        var configuration = await systemConfigurationRepository.GetByKeyAsync("MaxWeeklyHours", cancellationToken);
        return int.TryParse(configuration?.Value, out var parsed) && parsed > 0
            ? parsed
            : BusinessRules.DefaultMaxWeeklyHours;
    }

    private static ManagerProjectHealthDto MapHealthDto(Project project, ProjectHealthEvaluation evaluation)
    {
        return new ManagerProjectHealthDto(
            project.Id,
            project.Name,
            project.ClientName,
            project.Status,
            evaluation.HealthStatus,
            project.StartDate,
            project.EndDate,
            project.TotalStoryPoints,
            project.CompletedStoryPoints,
            FormatStoryPointProgress(project),
            evaluation.ActiveAllocationCount,
            evaluation.PreviousWeekLoggedHours,
            evaluation.PreviousWeekExpectedHours,
            evaluation.Signals);
    }

    internal static string FormatStoryPointProgress(Project project)
    {
        return project.TotalStoryPoints == 0
            ? "0/0"
            : $"{project.CompletedStoryPoints}/{project.TotalStoryPoints}";
    }

    internal static DateOnly GetPreviousWeekStart(DateOnly referenceDate)
    {
        return GetWeekStart(referenceDate).AddDays(-7);
    }

    internal static DateOnly GetWeekStart(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }

    private static bool DateRangesOverlap(DateOnly from1, DateOnly? to1, DateOnly from2, DateOnly to2)
    {
        var end1 = to1 ?? DateOnly.MaxValue;
        return from1 <= to2 && from2 <= end1;
    }

    internal sealed record ProjectHealthEvaluation(
        ProjectHealthStatus HealthStatus,
        IReadOnlyList<string> Signals,
        int ActiveAllocationCount,
        decimal PreviousWeekLoggedHours,
        decimal PreviousWeekExpectedHours);
}
