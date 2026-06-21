using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.DTOs.Manager;

namespace ProjectResourceManagement.Client.Screens.Manager;

/// <summary>
/// Combined allocation workflow: AI-assisted match, direct allocation, and end allocation.
/// </summary>
internal static class ManagerAllocateScreen
{
    public static Task RunAsync(HttpClient client) =>
        MenuLoop.RunAsync(
            "Allocate Resource",
            "Find, assign, or end team allocations",
            [
                new MenuItem("Find resource using AI (recommended)", ScreenRunner.Wrap(() => RunAiAssistedAsync(client))),
                new MenuItem("Allocate directly (I already know who I want)", ScreenRunner.Wrap(() => RunDirectAllocationAsync(client))),
                new MenuItem("End an existing allocation", ScreenRunner.Wrap(() => RunEndAllocationAsync(client)))
            ]);

    private static async Task RunAiAssistedAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Allocate Resource", "Step 1 — AI-assisted search");

        var projectId = ConsolePrompt.ReadRequiredInt("Project ID");
        var query = ConsolePrompt.ReadRequiredText("Describe your requirement");

        Console.WriteLine();
        Console.WriteLine("Searching... (AI matching in progress)");

        var response = await client.PostAsJsonAsync(
            "/api/manager/ai/skill-match",
            new AiSkillMatchRequest(query, projectId));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var result = await ApiHelper.ReadAsync<AiSkillMatchResponse>(response);
        if (result is null || result.Candidates.Count == 0)
        {
            ConsoleScreen.ShowWarning("No matching candidates returned.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("VERIFIED CANDIDATES (from your direct team)");
        Console.WriteLine(new string('─', 46));

        ConsoleTable.Print(
            ["#", "Emp ID", "Name", "Util %", "Score", "Matched Skills"],
            result.Candidates.Select((candidate, index) => new[]
            {
                (index + 1).ToString(),
                candidate.UserId.ToString(),
                candidate.FullName,
                candidate.CurrentUtilizationPercent.ToString("0.##") + "%",
                candidate.MatchScore.ToString(),
                candidate.MatchedSkills.Count == 0 ? "-" : string.Join(", ", candidate.MatchedSkills)
            }));

        foreach (var candidate in result.Candidates)
        {
            Console.WriteLine($" - {candidate.Explanation}");
        }

        Console.WriteLine();
        Console.WriteLine("AI commentary (explanation only — allocate using the table above):");
        Console.WriteLine(result.Summary);
        Console.WriteLine($"Mode: {(result.UsedFallback ? "Fallback (no LLM)" : $"LLM ({result.ProviderUsed})")}");
        Console.WriteLine();
        Console.WriteLine("Note: Only verified candidates above can be selected.");
        Console.Write("Select employee # (0 to cancel): ");
        if (!int.TryParse(Console.ReadLine(), out var selection) || selection < 0 || selection > result.Candidates.Count)
        {
            ConsoleScreen.ShowError("Invalid selection.");
            return;
        }

        if (selection == 0)
        {
            return;
        }

        var chosen = result.Candidates[selection - 1];
        await ConfirmAndCreateAllocationAsync(client, projectId, chosen.UserId, chosen.FullName);
    }

    private static async Task RunDirectAllocationAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Direct Allocation");

        var projectId = ConsolePrompt.ReadRequiredInt("Project ID");
        var userId = ConsolePrompt.ReadRequiredInt("User ID");

        await ConfirmAndCreateAllocationAsync(client, projectId, userId, $"User {userId}");
    }

    private static async Task ConfirmAndCreateAllocationAsync(
        HttpClient client,
        int projectId,
        int userId,
        string employeeLabel)
    {
        Console.WriteLine();
        Console.WriteLine($"── {employeeLabel} ─────────────────────────────────");

        var utilization = ConsolePrompt.ReadRequiredDecimal("Utilisation % (1-100)");
        var fromDate = ConsolePrompt.ReadDate("From date", DateOnly.FromDateTime(DateTime.UtcNow));
        var toDateRaw = ConsolePrompt.ReadOptionalText("To date (yyyy-MM-dd)");
        DateOnly? toDate = null;
        if (!string.IsNullOrWhiteSpace(toDateRaw))
        {
            if (!DateOnly.TryParse(toDateRaw, out var parsedToDate))
            {
                ConsoleScreen.ShowError("Invalid to date.");
                return;
            }

            toDate = parsedToDate;
        }

        if (!ConsolePrompt.ReadYesNo("Confirm allocation?"))
        {
            return;
        }

        var response = await client.PostAsJsonAsync(
            "/api/manager/allocations",
            new CreateAllocationRequest(projectId, userId, utilization, fromDate, toDate));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var created = await ApiHelper.ReadAsync<AllocationDetailDto>(response);
            ConsoleScreen.ShowSuccess(created is null
                ? "Allocation saved."
                : $"Allocation saved. {created.UserName} → {created.ProjectName} ({created.UtilizationPercentage}%).");
        }
    }

    private static async Task RunEndAllocationAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("End Allocation");

        var allocationId = ConsolePrompt.ReadRequiredInt("Allocation ID");

        if (!ConsolePrompt.ReadYesNo($"End allocation #{allocationId} as of today ({DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd})?"))
        {
            return;
        }

        var response = await client.PutAsync($"/api/manager/allocations/{allocationId}/end", null);
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            ConsoleScreen.ShowSuccess("Allocation ended successfully.");
        }
    }
}
