using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Timesheet;

namespace ProjectResourceManagement.Client.Screens.Manager;

/// <summary>
/// Manager view of team timesheets with optional week filter and missing-timesheet section.
/// </summary>
internal static class ManagerTimesheetsScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        await MenuLoop.RunAsync(
            "Team Timesheets",
            "View submissions, missing weeks, and restore frozen access",
            [
                new MenuItem("View Team Timesheets (by week)", ScreenRunner.Wrap(() => ViewByWeekAsync(client))),
                new MenuItem("Restore Frozen Timesheet Access", ScreenRunner.Wrap(() => RestoreFrozenAccessAsync(client))),
            ]);
    }

    private static async Task ViewByWeekAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Timesheets — My Team");

        var defaultWeek = WeekHelper.GetDefaultTimesheetWeekStart();
        var weekStart = ConsolePrompt.ReadDate("Filter by week start (yyyy-MM-dd)", defaultWeek);

        await ShowMissingTimesheetsAsync(client, weekStart);
        await ShowSubmittedTimesheetsAsync(client, weekStart);
    }

    private static async Task RestoreFrozenAccessAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Restore Frozen Timesheet Access");

        var response = await client.GetAsync("/api/manager/timesheets/frozen");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var frozenEmployees = await ApiHelper.ReadAsync<List<FrozenTimesheetEmployeeDto>>(response) ?? [];
        if (frozenEmployees.Count == 0)
        {
            ConsoleScreen.ShowInfo("No employees on your team have frozen timesheet access.");
            return;
        }

        ConsoleTable.Print(
            ["#", "User ID", "Employee", "Missing Week", "Reminders", "Frozen At"],
            frozenEmployees.Select((item, index) => new[]
            {
                (index + 1).ToString(),
                item.UserId.ToString(),
                item.FullName,
                item.MissingWeekStartDate?.ToString("yyyy-MM-dd") ?? "-",
                item.ReminderCount.ToString(),
                item.FrozenAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            }));

        Console.Write("Enter employee number to restore: ");
        if (!int.TryParse(Console.ReadLine(), out var selected) || selected < 1 || selected > frozenEmployees.Count)
        {
            ConsoleScreen.ShowError("Invalid selection.");
            return;
        }

        var employee = frozenEmployees[selected - 1];
        if (!ConsolePrompt.ReadYesNo($"Restore timesheet access for {employee.FullName}?"))
        {
            return;
        }

        var restoreResponse = await client.PutAsync($"/api/manager/timesheets/compliance/{employee.UserId}/restore", null);
        if (await ApiHelper.EnsureSuccessAsync(restoreResponse))
        {
            ConsoleScreen.ShowSuccess($"{employee.FullName} can submit timesheets again.");
        }
    }

    private static async Task ShowMissingTimesheetsAsync(HttpClient client, DateOnly weekStart)
    {
        var response = await client.GetAsync($"/api/manager/timesheets/missing?weekStartDate={weekStart:yyyy-MM-dd}");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var reminders = await ApiHelper.ReadAsync<List<MissingTimesheetReminderDto>>(response) ?? [];

        Console.WriteLine();
        Console.WriteLine($"MISSING TIMESHEETS — week of {weekStart:yyyy-MM-dd}");
        Console.WriteLine(new string('─', 46));

        if (reminders.Count == 0)
        {
            Console.WriteLine("(none — all team members submitted for this week)");
            return;
        }

        ConsoleTable.Print(
            ["User ID", "Employee", "Email", "Missing Week", "Reminders", "Frozen"],
            reminders.Select(item => new[]
            {
                item.UserId.ToString(),
                item.UserName,
                item.Email,
                item.WeekStartDate.ToString("yyyy-MM-dd"),
                item.ReminderCount.ToString(),
                ApiHelper.YesNo(item.IsTimesheetSubmissionFrozen)
            }));
    }

    private static async Task ShowSubmittedTimesheetsAsync(HttpClient client, DateOnly weekStart)
    {
        var url = $"/api/manager/timesheets?weekStartDate={weekStart:yyyy-MM-dd}";
        var response = await client.GetAsync(url);
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var timesheets = await ApiHelper.ReadAsync<List<TimesheetSummaryDto>>(response) ?? [];
        var filtered = timesheets
            .Where(item => item.WeekStartDate == weekStart)
            .OrderBy(item => item.UserName)
            .ToList();

        Console.WriteLine();
        Console.WriteLine($"SUBMITTED TIMESHEETS — week of {weekStart:yyyy-MM-dd}");
        Console.WriteLine(new string('─', 46));

        ConsoleTable.Print(
            ["Timesheet ID", "Employee", "Week Start", "Total Hours", "Status", "Submitted At"],
            filtered.Select(item => new[]
            {
                item.TimesheetId.ToString(),
                item.UserName,
                item.WeekStartDate.ToString("yyyy-MM-dd"),
                item.TotalHours.ToString("0.##"),
                item.Status.ToString(),
                item.SubmittedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            }));

        if (filtered.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.Write("[V] View timesheet detail by ID (blank to skip): ");
        var raw = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || ConsolePrompt.WantsToGoBack(raw))
        {
            return;
        }

        if (!int.TryParse(raw, out var timesheetId))
        {
            ConsoleScreen.ShowError("Invalid timesheet ID.");
            return;
        }

        await ShowTimesheetDetailAsync(client, timesheetId);
    }

    private static async Task ShowTimesheetDetailAsync(HttpClient client, int timesheetId)
    {
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

        ConsoleScreen.ShowHeader(
            $"Timesheet — {detail.UserName}",
            $"Week {detail.WeekStartDate:yyyy-MM-dd} | {detail.TotalHours:0.##}h | {detail.Status}");

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
}
