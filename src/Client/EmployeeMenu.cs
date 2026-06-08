using System.Net.Http.Json;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.DTOs.Timesheet;

namespace ProjectResourceManagement.Client;

internal static class EmployeeMenu
{
    public static async Task RunAsync(HttpClient client, LoginResponse session)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"Employee Menu  |  Welcome, {session.FullName}");
            Console.WriteLine("────────────────────────────────────────");
            Console.WriteLine(" 1. Submit weekly timesheet");
            Console.WriteLine(" 2. Timesheet history");
            Console.WriteLine(" 3. Timesheet detail");
            Console.WriteLine(" 4. My allocations");
            Console.WriteLine(" 5. Change password");
            Console.WriteLine(" 0. Logout");
            Console.Write("Choose option: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1": await SubmitTimesheetAsync(client); break;
                case "2": await ListHistoryAsync(client); break;
                case "3": await ShowDetailAsync(client); break;
                case "4": await ListAllocationsAsync(client); break;
                case "5": await ChangePasswordAsync(client, session); break;
                case "0": return;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    private static async Task SubmitTimesheetAsync(HttpClient client)
    {
        var defaultWeek = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        Console.Write($"Week start date (yyyy-MM-dd, default {defaultWeek:yyyy-MM-dd}): ");
        var weekRaw = Console.ReadLine();
        var weekStart = string.IsNullOrWhiteSpace(weekRaw)
            ? defaultWeek
            : DateOnly.Parse(weekRaw);

        var activeProjectsResponse = await client.GetAsync($"/api/employee/timesheets/active-projects?weekStartDate={weekStart:yyyy-MM-dd}");
        if (!await ApiHelper.EnsureSuccessAsync(activeProjectsResponse))
        {
            return;
        }

        var activeProjects = await ApiHelper.ReadAsync<List<ActiveProjectForTimesheetDto>>(activeProjectsResponse) ?? [];
        if (activeProjects.Count == 0)
        {
            Console.WriteLine("No active project allocations found for this week.");
            return;
        }

        Console.WriteLine("Active projects for this week:");
        ConsoleTable.Print(
            ["Project ID", "Project", "Allocation %", "Max Hours"],
            activeProjects.Select(project => new[]
            {
                project.ProjectId.ToString(),
                project.ProjectName,
                project.AllocationPercent.ToString("0.##"),
                project.MaxHoursForWeek.ToString("0.##")
            }));

        var entries = new List<SubmitTimesheetEntryRequest>();
        foreach (var project in activeProjects)
        {
            Console.Write($"Hours for {project.ProjectName} (0 to skip): ");
            if (!decimal.TryParse(Console.ReadLine(), out var hours) || hours <= 0)
            {
                continue;
            }

            entries.Add(new SubmitTimesheetEntryRequest(project.ProjectId, hours, string.Empty, []));
        }

        if (entries.Count == 0)
        {
            Console.WriteLine("No hours entered.");
            return;
        }

        var response = await client.PostAsJsonAsync(
            "/api/employee/timesheets",
            new SubmitTimesheetRequest(weekStart, entries));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var created = await ApiHelper.ReadAsync<TimesheetDetailDto>(response);
            Console.WriteLine(created is null
                ? "Timesheet submitted."
                : $"Timesheet submitted for week {created.WeekStartDate:yyyy-MM-dd} with {created.TotalHours} hours.");
        }
    }

    private static async Task ListHistoryAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/employee/timesheets");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var timesheets = await ApiHelper.ReadAsync<List<TimesheetSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Timesheet ID", "Week Start", "Total Hours", "Status", "Submitted At"],
            timesheets.Select(item => new[]
            {
                item.TimesheetId.ToString(),
                item.WeekStartDate.ToString("yyyy-MM-dd"),
                item.TotalHours.ToString("0.##"),
                item.Status.ToString(),
                item.SubmittedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            }));
    }

    private static async Task ShowDetailAsync(HttpClient client)
    {
        Console.Write("Week start date (yyyy-MM-dd): ");
        if (!DateOnly.TryParse(Console.ReadLine(), out var weekStart))
        {
            Console.WriteLine("Invalid date.");
            return;
        }

        var response = await client.GetAsync($"/api/employee/timesheets/{weekStart:yyyy-MM-dd}");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var detail = await ApiHelper.ReadAsync<TimesheetDetailDto>(response);
        if (detail is null)
        {
            return;
        }

        Console.WriteLine($"Timesheet {detail.TimesheetId} | Week {detail.WeekStartDate:yyyy-MM-dd} | Total {detail.TotalHours}h | {detail.Status}");
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

    private static async Task ListAllocationsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/employee/allocations");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var allocations = await ApiHelper.ReadAsync<List<EmployeeAllocationDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Alloc ID", "Project", "Util %", "From", "To", "Status"],
            allocations.Select(item => new[]
            {
                item.AllocationId.ToString(),
                item.ProjectName,
                item.UtilizationPercentage.ToString("0.##"),
                item.FromDate.ToString("yyyy-MM-dd"),
                item.ToDate?.ToString("yyyy-MM-dd") ?? "-",
                item.Status.ToString()
            }));
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

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }
}
