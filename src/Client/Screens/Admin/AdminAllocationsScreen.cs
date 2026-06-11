using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Client.Screens.Admin;

/// <summary>
/// Read-only view of the organization-wide allocation matrix for Admin users.
/// </summary>
internal static class AdminAllocationsScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("All Allocations", "Organization-wide allocation matrix");

        var response = await client.GetAsync("/api/allocations/matrix");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var rows = await ApiHelper.ReadAsync<List<AllocationMatrixRowDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Alloc ID", "User", "Project", "Manager", "Util %", "From", "To", "Status"],
            rows.Select(row => new[]
            {
                row.AllocationId.ToString(),
                row.UserName,
                row.ProjectName,
                row.ManagerName,
                row.UtilizationPercentage.ToString("0.##"),
                row.FromDate.ToString("yyyy-MM-dd"),
                row.ToDate?.ToString("yyyy-MM-dd") ?? "-",
                row.Status
            }));

        if (rows.Count == 0)
        {
            ConsoleScreen.ShowInfo("No allocations found.");
        }
    }
}
