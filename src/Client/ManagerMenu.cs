using System.Net.Http.Json;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.DTOs.Timesheet;

namespace ProjectResourceManagement.Client;

internal static class ManagerMenu
{
    public static async Task RunAsync(HttpClient client, LoginResponse session)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"Manager Menu  |  Welcome, {session.FullName}");
            Console.WriteLine("────────────────────────────────────────");
            Console.WriteLine(" 1. Resource dashboard (direct team)");
            Console.WriteLine(" 2. Allocate team member to project");
            Console.WriteLine(" 3. End allocation");
            Console.WriteLine(" 4. My projects");
            Console.WriteLine(" 5. Team timesheets");
            Console.WriteLine(" 6. Missing timesheet reminders");
            Console.WriteLine(" 7. Change password");
            Console.WriteLine(" 0. Logout");
            Console.Write("Choose option: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1": await ShowDashboardAsync(client); break;
                case "2": await AllocateTeamMemberAsync(client); break;
                case "3": await EndAllocationAsync(client); break;
                case "4": await ListProjectsAsync(client); break;
                case "5": await ListTeamTimesheetsAsync(client); break;
                case "6": await ShowMissingTimesheetsAsync(client); break;
                case "7": await ChangePasswordAsync(client, session); break;
                case "0": return;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    private static async Task ShowDashboardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/manager/dashboard");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var rows = await ApiHelper.ReadAsync<List<ResourceDashboardRowDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Emp ID", "Name", "Department", "Designation", "Util %", "Category", "Active Allocations"],
            rows.Select(row => new[]
            {
                row.EmployeeId.ToString(),
                row.FullName,
                row.Department,
                row.Designation,
                row.CurrentUtilizationPercent.ToString("0.##"),
                row.Category.ToString(),
                row.ActiveAllocationsSummary
            }));

        if (rows.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("No direct team members found. Ask Admin to assign employees to you.");
        }
    }

    private static async Task ListProjectsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/manager/projects");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var projects = await ApiHelper.ReadAsync<List<ManagerProjectOptionDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Project ID", "Name", "Client", "Status", "Start", "End"],
            projects.Select(project => new[]
            {
                project.ProjectId.ToString(),
                project.Name,
                project.ClientName,
                project.Status.ToString(),
                project.StartDate.ToString("yyyy-MM-dd"),
                project.EndDate.ToString("yyyy-MM-dd")
            }));
    }

    private static async Task AllocateTeamMemberAsync(HttpClient client)
    {
        Console.WriteLine("Tip: Dashboard shows your direct team. Admin must assign employees to you first.");
        await ShowDashboardAsync(client);

        Console.Write("Project ID: ");
        if (!int.TryParse(Console.ReadLine(), out var projectId))
        {
            Console.WriteLine("Invalid project ID.");
            return;
        }

        Console.Write("Employee ID (from dashboard): ");
        if (!int.TryParse(Console.ReadLine(), out var employeeId))
        {
            Console.WriteLine("Invalid employee ID.");
            return;
        }

        Console.Write("Utilization % (1-100): ");
        if (!decimal.TryParse(Console.ReadLine(), out var utilization))
        {
            Console.WriteLine("Invalid utilization.");
            return;
        }

        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
        Console.Write("To date (yyyy-MM-dd, blank = open-ended): ");
        var toDateRaw = Console.ReadLine();
        DateOnly? toDate = null;
        if (!string.IsNullOrWhiteSpace(toDateRaw))
        {
            if (!DateOnly.TryParse(toDateRaw, out var parsedToDate))
            {
                Console.WriteLine("Invalid to date.");
                return;
            }

            toDate = parsedToDate;
        }

        var response = await client.PostAsJsonAsync(
            "/api/manager/allocations",
            new CreateAllocationRequest(projectId, employeeId, utilization, fromDate, toDate));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var created = await ApiHelper.ReadAsync<AllocationDetailDto>(response);
            Console.WriteLine(created is null
                ? "Allocation created."
                : $"Allocated {created.EmployeeName} to {created.ProjectName} at {created.UtilizationPercentage}%.");
        }
    }

    private static async Task EndAllocationAsync(HttpClient client)
    {
        Console.Write("Allocation ID: ");
        if (!int.TryParse(Console.ReadLine(), out var allocationId))
        {
            Console.WriteLine("Invalid allocation ID.");
            return;
        }

        var response = await client.PutAsync($"/api/manager/allocations/{allocationId}/end", null);
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            Console.WriteLine("Allocation ended successfully.");
        }
    }

    private static async Task ListTeamTimesheetsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/manager/timesheets");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var timesheets = await ApiHelper.ReadAsync<List<TimesheetSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Timesheet ID", "Employee", "Week Start", "Total Hours", "Status", "Submitted At"],
            timesheets.Select(item => new[]
            {
                item.TimesheetId.ToString(),
                item.EmployeeName,
                item.WeekStartDate.ToString("yyyy-MM-dd"),
                item.TotalHours.ToString("0.##"),
                item.Status.ToString(),
                item.SubmittedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            }));

        if (timesheets.Count == 0)
        {
            return;
        }

        Console.Write("Enter Timesheet ID for detail (blank to skip): ");
        if (!int.TryParse(Console.ReadLine(), out var timesheetId))
        {
            return;
        }

        var detailResponse = await client.GetAsync($"/api/manager/timesheets/{timesheetId}");
        if (!await ApiHelper.EnsureSuccessAsync(detailResponse))
        {
            return;
        }

        var detail = await ApiHelper.ReadAsync<TimesheetDetailDto>(detailResponse);
        if (detail is null)
        {
            return;
        }

        Console.WriteLine($"Detail for {detail.EmployeeName} | Week {detail.WeekStartDate:yyyy-MM-dd}");
        ConsoleTable.Print(
            ["Entry ID", "Project", "Hours", "Notes", "Activity Tags"],
            detail.Entries.Select(entry => new[]
            {
                entry.EntryId.ToString(),
                entry.ProjectName,
                entry.HoursWorked.ToString("0.##"),
                string.IsNullOrWhiteSpace(entry.Notes) ? "-" : entry.Notes,
                entry.ActivityTags.Count == 0 ? "-" : string.Join(", ", entry.ActivityTags)
            }));
    }

    private static async Task ShowMissingTimesheetsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/manager/timesheets/missing");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var reminders = await ApiHelper.ReadAsync<List<MissingTimesheetReminderDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Employee ID", "Employee", "Email", "Missing Week"],
            reminders.Select(item => new[]
            {
                item.EmployeeId.ToString(),
                item.EmployeeName,
                item.Email,
                item.WeekStartDate.ToString("yyyy-MM-dd")
            }));

        if (reminders.Count == 0)
        {
            Console.WriteLine("No missing timesheets for the previous week.");
        }
    }

    private static async Task ChangePasswordAsync(HttpClient client, LoginResponse session)
    {
        Console.Write("New password: ");
        var newPassword = Console.ReadLine() ?? string.Empty;
        Console.Write("Confirm password: ");
        var confirmPassword = Console.ReadLine() ?? string.Empty;

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordRequest(session.UserId, newPassword, confirmPassword));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            Console.WriteLine("Password changed successfully.");
        }
    }
}
