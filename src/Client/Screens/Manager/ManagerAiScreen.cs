using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.DTOs.Manager;

namespace ProjectResourceManagement.Client.Screens.Manager;

/// <summary>
/// Standalone AI assistant entry point for skill match and project risk summary.
/// </summary>
internal static class ManagerAiScreen
{
    public static Task RunAsync(HttpClient client) =>
        MenuLoop.RunAsync(
            "AI Assistant",
            "Skill match and project risk analysis",
            [
                new MenuItem("Skill Match — find best employees for a requirement", ScreenRunner.Wrap(() => RunSkillMatchAsync(client))),
                new MenuItem("Risk Summary — get health analysis for a project", ScreenRunner.Wrap(() => RunRiskSummaryAsync(client)))
            ]);

    private static async Task RunSkillMatchAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Skill Match");

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
        Console.WriteLine(result.Summary);
        Console.WriteLine($"Mode: {(result.UsedFallback ? "Fallback (no LLM)" : $"LLM ({result.ProviderUsed})")}");
        Console.WriteLine();

        var index = 1;
        foreach (var candidate in result.Candidates)
        {
            Console.WriteLine($"  {index}.  {candidate.FullName}");
            Console.WriteLine($"      Reason: {candidate.Explanation}");
            if (candidate.MatchedSkills.Count > 0)
            {
                Console.WriteLine($"      Skills: {string.Join(", ", candidate.MatchedSkills)}");
            }

            index++;
        }

        Console.WriteLine();
        Console.WriteLine("Note: These are AI-generated suggestions. Verify before allocating.");
        Console.WriteLine("[A] Go to Allocate Resource is available from the Manager menu.");
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
