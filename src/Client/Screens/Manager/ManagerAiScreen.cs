using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Client.Screens.Manager;

/// <summary>
/// Standalone AI assistant entry point for skill match, team match, and project risk summary.
/// </summary>
internal static class ManagerAiScreen
{
    public static Task RunAsync(HttpClient client) =>
        MenuLoop.RunAsync(
            "AI Assistant",
            "Skill match, team match, and project risk analysis",
            [
                new MenuItem("Skill Match — find employees on your direct team", ScreenRunner.Wrap(() => RunSkillMatchAsync(client))),
                new MenuItem("Team Match — organization-wide multi-role search", ScreenRunner.Wrap(() => RunTeamMatchAsync(client))),
                new MenuItem("Risk Summary — get health analysis for a project", ScreenRunner.Wrap(() => RunRiskSummaryAsync(client)))
            ]);

    private static async Task RunSkillMatchAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Skill Match — Direct Team");

        var query = ConsolePrompt.ReadRequiredText("Describe your project requirement");
        var projectId = ConsolePrompt.ReadOptionalInt("Optional project ID");

        Console.WriteLine();
        Console.WriteLine("Searching... (calling AI)");

        var response = await client.PostAsJsonAsync(
            "/api/manager/ai/skill-match",
            new AiSkillMatchRequest(query, projectId));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var result = await ApiHelper.ReadAsync<AiSkillMatchResponse>(response);
        if (result is null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Verified candidates (from your direct team):");
        Console.WriteLine(new string('─', 46));

        if (result.Candidates.Count == 0)
        {
            ConsoleScreen.ShowInfo("No matching employees found on your team for this requirement.");
            Console.WriteLine();
            Console.WriteLine(result.Summary);
            Console.WriteLine($"Mode: {(result.UsedFallback ? "Fallback (no LLM)" : $"LLM ({result.ProviderUsed})")}");
            return;
        }

        var index = 1;
        foreach (var candidate in result.Candidates)
        {
            Console.WriteLine($"  {index}.  {candidate.FullName} (UserId {candidate.UserId})");
            Console.WriteLine($"      Reason: {candidate.Explanation}");
            if (candidate.MatchedSkills.Count > 0)
            {
                Console.WriteLine($"      Skills: {string.Join(", ", candidate.MatchedSkills)}");
            }

            index++;
        }

        Console.WriteLine();
        Console.WriteLine("AI commentary (for explanation only — use the verified list above to allocate):");
        Console.WriteLine(result.Summary);
        Console.WriteLine($"Mode: {(result.UsedFallback ? "Fallback (no LLM)" : $"LLM ({result.ProviderUsed})")}");
        Console.WriteLine();

        Console.WriteLine("Note: Searches only employees who report to you. Verify before allocating.");
        Console.WriteLine("[A] Go to Allocate Resource is available from the Manager menu.");
    }

    private static async Task RunTeamMatchAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Team Match — Organization-wide");

        var roles = new List<TeamRoleRequirementDto>();
        Console.WriteLine("Define each role for the team. Leave role title blank when finished.");
        Console.WriteLine();

        while (true)
        {
            var roleTitle = ConsolePrompt.ReadOptionalText($"Role {roles.Count + 1} title (blank to finish)");
            if (string.IsNullOrWhiteSpace(roleTitle))
            {
                if (roles.Count == 0)
                {
                    ConsoleScreen.ShowError("At least one role is required.");
                    continue;
                }

                break;
            }

            var skillName = ConsolePrompt.ReadRequiredText("Required skill name");
            var proficiency = ReadRequiredProficiencyLevel();
            roles.Add(new TeamRoleRequirementDto(roleTitle, skillName, proficiency));
            Console.WriteLine();
        }

        var projectId = ConsolePrompt.ReadOptionalInt("Optional project ID");
        var context = ConsolePrompt.ReadOptionalText("Optional context (e.g. new banking portal)");

        Console.WriteLine();
        Console.WriteLine("Searching organization... (single-pass team match)");

        var response = await client.PostAsJsonAsync(
            "/api/manager/ai/team-match",
            new AiTeamMatchRequest(roles, projectId, string.IsNullOrWhiteSpace(context) ? null : context));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var result = await ApiHelper.ReadAsync<AiTeamMatchResponse>(response);
        if (result is null)
        {
            return;
        }

        Console.WriteLine();
        if (!string.IsNullOrWhiteSpace(result.ProjectName))
        {
            Console.WriteLine($"Project: {result.ProjectName}");
        }

        Console.WriteLine($"Filled: {result.FilledCount}/{result.TotalRoles} roles");
        Console.WriteLine();
        Console.WriteLine(result.Summary);
        Console.WriteLine($"Mode: {(result.UsedFallback ? "Fallback (no LLM)" : $"LLM ({result.ProviderUsed})")}");
        Console.WriteLine();
        Console.WriteLine("Role details:");

        foreach (var roleResult in result.RoleResults)
        {
            Console.WriteLine();
            if (roleResult.IsFilled && roleResult.SuggestedCandidate is not null)
            {
                var candidate = roleResult.SuggestedCandidate;
                Console.WriteLine($"  ✓ {roleResult.RoleTitle} ({roleResult.RequiredSkillName}, min {roleResult.MinimumProficiency})");
                Console.WriteLine($"      {candidate.FullName} — {candidate.Department}, {candidate.Designation}");
                Console.WriteLine($"      Utilization: {candidate.CurrentUtilizationPercent:0.##}% | Status: {candidate.Status}");
                Console.WriteLine($"      Reason: {candidate.Explanation}");
                continue;
            }

            var gapLabel = roleResult.GapType switch
            {
                TeamRoleGapType.SkillGap => "Skill gap",
                TeamRoleGapType.AvailabilityGap => "Availability gap",
                _ => "Unfilled"
            };

            Console.WriteLine($"  ✗ {roleResult.RoleTitle} ({roleResult.RequiredSkillName}, min {roleResult.MinimumProficiency}) — {gapLabel}");
            Console.WriteLine($"      {roleResult.GapReason}");
            if (roleResult.AvailableFromDate is not null)
            {
                Console.WriteLine($"      Earliest availability: {roleResult.AvailableFromDate:yyyy-MM-dd}");
            }

            if (roleResult.SuggestedCandidate is not null)
            {
                var candidate = roleResult.SuggestedCandidate;
                Console.WriteLine($"      Closest match: {candidate.FullName} ({candidate.Department})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Note: Partial results are shown when not all roles are filled.");
        Console.WriteLine("Allocation is your decision — use Manager → Allocate Resource when ready.");
    }

    private static ProficiencyLevel ReadRequiredProficiencyLevel()
    {
        while (true)
        {
            Console.Write("Minimum proficiency (Beginner/Intermediate/Advanced/Expert): ");
            if (Enum.TryParse<ProficiencyLevel>(Console.ReadLine(), true, out var level))
            {
                return level;
            }

            Console.WriteLine("  Invalid proficiency. Try again.");
        }
    }

    private static async Task RunRiskSummaryAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Risk Summary");

        var projectsResponse = await client.GetAsync("/api/manager/projects");
        if (!await ApiHelper.EnsureSuccessAsync(projectsResponse))
        {
            return;
        }

        var projects = await ApiHelper.ReadAsync<List<ManagerProjectOptionDto>>(projectsResponse) ?? [];
        if (projects.Count == 0)
        {
            ConsoleScreen.ShowInfo("No projects found.");
            return;
        }

        for (var index = 0; index < projects.Count; index++)
        {
            var project = projects[index];
            Console.WriteLine($"  {index + 1}.  {project.Name}    {project.HealthStatus}");
        }

        Console.WriteLine();
        Console.Write("Enter project number: ");
        if (!int.TryParse(Console.ReadLine(), out var selected) || selected < 1 || selected > projects.Count)
        {
            ConsoleScreen.ShowError("Invalid project number.");
            return;
        }

        var projectId = projects[selected - 1].ProjectId;
        Console.WriteLine();
        Console.WriteLine("Generating AI summary...");

        var response = await client.PostAsJsonAsync(
            "/api/manager/ai/project-risk-summary",
            new AiProjectRiskSummaryRequest(projectId));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var result = await ApiHelper.ReadAsync<AiProjectRiskSummaryResponse>(response);
        if (result is null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine(result.Summary);
        Console.WriteLine();
        Console.WriteLine("Note: AI-generated from current milestone and timesheet data.");
    }
}
