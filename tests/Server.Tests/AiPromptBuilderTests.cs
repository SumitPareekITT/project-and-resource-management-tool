using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Server.Services.Ai.Prompts;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class AiPromptBuilderTests
{
    [Fact]
    public void SkillMatchPromptBuilder_IncludesOnlyPreFilteredCandidates()
    {
        var builder = new SkillMatchPromptBuilder();
        var candidates = new List<SkillMatchCandidate>
        {
            new()
            {
                Profile = new UserProfile
                {
                    UserId = 10,
                    FullName = "Alex Backend",
                    Department = "Engineering",
                    Designation = "Senior Developer",
                    ResourceStatus = EmployeeStatus.PartiallyAllocated,
                    CurrentUtilizationPercent = 40
                },
                MatchScore = 7,
                HasQueryMatch = true,
                MatchedSkills = ["Backend API Development (Advanced)"]
            }
        };

        var prompt = builder.Build("need backend api developer", candidates);

        Assert.Contains("Verified direct-team candidates", prompt.UserPrompt);
        Assert.Contains("Never invent employees", prompt.SystemInstruction);
    }

    [Fact]
    public void ProjectRiskPromptBuilder_IncludesOnlyProjectFacts()
    {
        var builder = new ProjectRiskPromptBuilder();
        var facts = new ProjectRiskFacts(
            ProjectId: 1,
            ProjectName: "Apollo",
            ClientName: "Acme",
            Status: ProjectStatus.Active,
            HealthStatus: ProjectHealthStatus.AtRisk,
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 12, 31),
            TotalStoryPoints: 100,
            CompletedStoryPoints: 40,
            ActiveAllocationCount: 2,
            PreviousWeekLoggedHours: 12,
            PreviousWeekExpectedHours: 40,
            MilestoneLines: ["Release: due 2026-07-01, status InProgress, SP 5/20"],
            AllocationLines: ["Alex Backend: 50.00% from 2026-06-01 to open"]);

        var prompt = builder.Build(facts);

        Assert.Contains("Project: Apollo (Acme)", prompt.UserPrompt);
        Assert.Contains("Health: AtRisk", prompt.UserPrompt);
        Assert.Contains("Release: due 2026-07-01", prompt.UserPrompt);
        Assert.Contains("Alex Backend: 50.00%", prompt.UserPrompt);
        Assert.Contains("only the factual lines provided", prompt.SystemInstruction);
    }
}
