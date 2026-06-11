using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Client.Screens.Manager;

/// <summary>
/// Shows the manager's direct-team resource dashboard grouped by utilization category.
/// </summary>
internal static class ManagerDashboardScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Resource Dashboard", DateTime.UtcNow.ToString("MMM yyyy"));

        var response = await client.GetAsync("/api/manager/dashboard");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var rows = await ApiHelper.ReadAsync<List<ResourceDashboardRowDto>>(response) ?? [];
        if (rows.Count == 0)
        {
            ConsoleScreen.ShowInfo("No direct team members found. Ask Admin to assign employees to you.");
            return;
        }

        PrintCategorySection("ON BENCH", rows, ResourceDashboardCategory.Bench);
        PrintCategorySection("PARTIALLY ALLOCATED", rows, ResourceDashboardCategory.PartiallyAllocated);
        PrintCategorySection("FULLY ALLOCATED", rows, ResourceDashboardCategory.Allocated);
        PrintCategorySection("OVERALLOCATED", rows, ResourceDashboardCategory.Overallocated);

        var benchCount = rows.Count(row => row.Category == ResourceDashboardCategory.Bench);
        var partialCount = rows.Count(row => row.Category == ResourceDashboardCategory.PartiallyAllocated);
        Console.WriteLine();
        Console.WriteLine($"Bench: {benchCount}   |   Partial: {partialCount}");
        ConsoleScreen.ShowBackHint();

        Console.Write("Enter [D] to drill into employee details, or [B] to go back: ");
        var choice = Console.ReadLine()?.Trim();
        if (string.Equals(choice, "D", StringComparison.OrdinalIgnoreCase))
        {
            DrillDown(rows);
            return;
        }

        if (!ConsolePrompt.WantsToGoBack(choice))
        {
            ConsoleScreen.ShowError("Invalid option.");
        }
    }

    private static void PrintCategorySection(
        string title,
        IReadOnlyList<ResourceDashboardRowDto> rows,
        ResourceDashboardCategory category)
    {
        var categoryRows = rows.Where(row => row.Category == category).ToList();
        Console.WriteLine();
        Console.WriteLine($"{title}  ({categoryRows.Count} employees)");
        Console.WriteLine(new string('─', 46));

        if (categoryRows.Count == 0)
        {
            Console.WriteLine("(none)");
            return;
        }

        ConsoleTable.Print(
            ["ID", "Name", "Department", "Designation", "Util %", "Active Allocations"],
            categoryRows.Select(row => new[]
            {
                row.UserId.ToString(),
                row.FullName,
                row.Department,
                row.Designation,
                row.CurrentUtilizationPercent.ToString("0.##") + "%",
                string.IsNullOrWhiteSpace(row.ActiveAllocationsSummary) ? "-" : row.ActiveAllocationsSummary
            }));
    }

    private static void DrillDown(IReadOnlyList<ResourceDashboardRowDto> rows)
    {
        ConsoleScreen.ShowHeader("Employee Detail");

        var userId = ConsolePrompt.ReadRequiredInt("Enter User ID");
        var row = rows.FirstOrDefault(item => item.UserId == userId);
        if (row is null)
        {
            ConsoleScreen.ShowError("User ID not found on your dashboard.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"── {row.FullName} ─────────────────────────────────");
        Console.WriteLine($"Department      : {row.Department}");
        Console.WriteLine($"Designation     : {row.Designation}");
        Console.WriteLine($"Current Status  : {row.Category} ({row.CurrentUtilizationPercent:0.##}%)");
        Console.WriteLine($"Active Allocations: {row.ActiveAllocationsSummary}");
    }
}
