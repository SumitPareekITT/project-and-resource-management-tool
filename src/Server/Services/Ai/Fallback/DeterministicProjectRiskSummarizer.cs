using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Fallback;

public sealed class DeterministicProjectRiskSummarizer
{
    public string Summarize(ProjectRiskFacts facts)
    {
        var lines = new List<string>
        {
            $"Project {facts.ProjectName} is {facts.Status} with health {facts.HealthStatus}.",
            $"Story-point progress: {facts.CompletedStoryPoints}/{facts.TotalStoryPoints}.",
            $"Active allocations: {facts.ActiveAllocationCount}.",
            $"Previous-week effort: {facts.PreviousWeekLoggedHours:0.##}/{facts.PreviousWeekExpectedHours:0.##} hours."
        };

        if (facts.HealthStatus == ProjectHealthStatus.AtRisk)
        {
            lines.Add("Health is AtRisk. Review overdue milestones, staffing, and timesheet gaps immediately.");
        }
        else if (facts.HealthStatus == ProjectHealthStatus.Attention)
        {
            lines.Add("Health needs attention. Validate milestone dates and team capacity this week.");
        }
        else
        {
            lines.Add("No critical health flag is present in current project facts.");
        }

        if (facts.MilestoneLines.Count > 0)
        {
            lines.Add($"Nearest milestone context: {facts.MilestoneLines[0]}");
        }

        return string.Join(" ", lines);
    }

    public IReadOnlyList<string> ToFactLines(ProjectRiskFacts facts)
    {
        var lines = new List<string>
        {
            $"Status={facts.Status}; Health={facts.HealthStatus}",
            $"StoryPoints={facts.CompletedStoryPoints}/{facts.TotalStoryPoints}",
            $"ActiveAllocations={facts.ActiveAllocationCount}",
            $"PreviousWeekHours={facts.PreviousWeekLoggedHours:0.##}/{facts.PreviousWeekExpectedHours:0.##}"
        };

        lines.AddRange(facts.MilestoneLines.Select(line => $"Milestone: {line}"));
        lines.AddRange(facts.AllocationLines.Select(line => $"Allocation: {line}"));
        return lines;
    }
}
