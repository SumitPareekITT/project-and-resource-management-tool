using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.DTOs.Manager;

namespace ProjectResourceManagement.Client.Screens.Manager;

/// <summary>
/// Lists manager-owned projects and shows health detail with optional AI risk summary.
/// </summary>
internal static class ManagerProjectsScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("My Projects");

        var projectsResponse = await client.GetAsync("/api/manager/projects");
        if (!await ApiHelper.EnsureSuccessAsync(projectsResponse))
        {
            return;
        }

        var projects = await ApiHelper.ReadAsync<List<ManagerProjectOptionDto>>(projectsResponse) ?? [];
        if (projects.Count == 0)
        {
            ConsoleScreen.ShowInfo("No active or planned projects found for your account.");
            return;
        }

        ConsoleTable.Print(
            ["#", "Project ID", "Name", "Client", "Status", "Health", "End Date"],
            projects.Select((project, index) => new[]
            {
                (index + 1).ToString(),
                project.ProjectId.ToString(),
                project.Name,
                project.ClientName,
                project.Status.ToString(),
                project.HealthStatus.ToString(),
                project.EndDate.ToString("yyyy-MM-dd")
            }));

        Console.WriteLine();
        Console.Write("Select project number for details (blank to go back): ");
        var raw = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || ConsolePrompt.WantsToGoBack(raw))
        {
            return;
        }

        if (!int.TryParse(raw, out var selected) || selected < 1 || selected > projects.Count)
        {
            ConsoleScreen.ShowError("Invalid project number.");
            return;
        }

        await ShowProjectDetailAsync(client, projects[selected - 1]);
    }

    private static async Task ShowProjectDetailAsync(HttpClient client, ManagerProjectOptionDto project)
    {
        var healthResponse = await client.GetAsync("/api/manager/projects/health");
        if (!await ApiHelper.EnsureSuccessAsync(healthResponse))
        {
            return;
        }

        var healthProjects = await ApiHelper.ReadAsync<List<ManagerProjectHealthDto>>(healthResponse) ?? [];
        var health = healthProjects.FirstOrDefault(item => item.ProjectId == project.ProjectId);
        if (health is null)
        {
            ConsoleScreen.ShowError("Health data not found for this project.");
            return;
        }

        while (true)
        {
            ConsoleScreen.ShowHeader(project.Name, $"Health: {health.HealthStatus}");

            Console.WriteLine($"Client          : {health.ClientName}");
            Console.WriteLine($"Status          : {health.Status}");
            Console.WriteLine($"Story Points    : {health.StoryPointProgress}");
            Console.WriteLine($"Period          : {health.StartDate:yyyy-MM-dd} to {health.EndDate:yyyy-MM-dd}");
            Console.WriteLine($"Allocations     : {health.ActiveAllocationCount}");
            Console.WriteLine($"Prev Week Hrs   : {health.PreviousWeekLoggedHours:0.##} / {health.PreviousWeekExpectedHours:0.##} expected");
            Console.WriteLine();

            if (health.HealthSignals.Count == 0)
            {
                Console.WriteLine("Risk Flags: (none reported)");
            }
            else
            {
                Console.WriteLine("Risk Flags:");
                foreach (var signal in health.HealthSignals)
                {
                    Console.WriteLine($"  - {signal}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("[A] Get AI Risk Summary     [B] Back");
            Console.Write("Enter choice: ");
            var choice = Console.ReadLine()?.Trim();

            if (ConsolePrompt.WantsToGoBack(choice))
            {
                return;
            }

            if (string.Equals(choice, "A", StringComparison.OrdinalIgnoreCase))
            {
                await ShowAiRiskSummaryAsync(client, project.ProjectId);
                continue;
            }

            ConsoleScreen.ShowError("Invalid option.");
        }
    }

    private static async Task ShowAiRiskSummaryAsync(HttpClient client, int projectId)
    {
        ConsoleScreen.ShowHeader("AI Risk Summary", "Generating summary...");

        var response = await client.PostAsJsonAsync(
            "/api/manager/ai/project-risk-summary",
            new AiProjectRiskSummaryRequest(projectId));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            ConsoleScreen.Pause();
            return;
        }

        var result = await ApiHelper.ReadAsync<AiProjectRiskSummaryResponse>(response);
        if (result is null)
        {
            ConsoleScreen.Pause();
            return;
        }

        Console.WriteLine($"{result.ProjectName} | Health: {result.HealthStatus}");
        Console.WriteLine($"Mode: {(result.UsedFallback ? "Fallback (no LLM)" : $"LLM ({result.ProviderUsed})")}");
        Console.WriteLine();
        Console.WriteLine("Facts used:");
        foreach (var fact in result.FactLines)
        {
            Console.WriteLine($" - {fact}");
        }

        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine(result.Summary);
        Console.WriteLine();
        Console.WriteLine("Note: AI-generated from milestone and timesheet data.");

        ConsoleScreen.Pause();
    }
}
