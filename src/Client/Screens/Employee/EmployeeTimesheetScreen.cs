using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.DTOs.Timesheet;

namespace ProjectResourceManagement.Client.Screens.Employee;

/// <summary>
/// Employee timesheet submit and history flows in one screen class.
/// </summary>
internal static class EmployeeTimesheetScreen
{
    public static async Task RunSubmitAsync(HttpClient client, LoginResponse session)
    {
        ConsoleScreen.ShowHeader("Submit Timesheet", $"Employee: {session.FullName}");

        var reminderResponse = await client.GetAsync("/api/employee/timesheets/missing-reminder");
        if (await ApiHelper.EnsureSuccessAsync(reminderResponse))
        {
            var reminder = await ApiHelper.ReadAsync<EmployeeTimesheetReminderDto>(reminderResponse);
            if (reminder is { IsTimesheetSubmissionFrozen: true })
            {
                ConsoleScreen.ShowError(reminder.Message ?? "Timesheet submission is frozen. Contact your manager.");
                ConsoleScreen.Pause();
                return;
            }
        }

        var defaultWeek = WeekHelper.GetDefaultTimesheetWeekStart();
        var weekStart = ConsolePrompt.ReadDate("Week start date (yyyy-MM-dd)", defaultWeek);

        var activeProjectsResponse = await client.GetAsync(
            $"/api/employee/timesheets/active-projects?weekStartDate={weekStart:yyyy-MM-dd}");
        if (!await ApiHelper.EnsureSuccessAsync(activeProjectsResponse))
        {
            ConsoleScreen.Pause();
            return;
        }

        var activeProjects = await ApiHelper.ReadAsync<List<ActiveProjectForTimesheetDto>>(activeProjectsResponse) ?? [];
        if (activeProjects.Count == 0)
        {
            ConsoleScreen.ShowWarning("No active project allocations found for this week.");
            ConsoleScreen.Pause();
            return;
        }

        Console.WriteLine();
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

        var activityTags = await LoadActivityTagsAsync(client);
        var entries = new List<SubmitTimesheetEntryRequest>();

        foreach (var project in activeProjects)
        {
            Console.WriteLine();
            Console.Write($"Hours for {project.ProjectName} (0 to skip): ");
            if (!decimal.TryParse(Console.ReadLine(), out var hours) || hours <= 0)
            {
                continue;
            }

            var notes = ConsolePrompt.ReadOptionalText("Notes");
            var selectedTagIds = PickActivityTags(activityTags);
            entries.Add(new SubmitTimesheetEntryRequest(project.ProjectId, hours, notes, selectedTagIds));
        }

        if (entries.Count == 0)
        {
            ConsoleScreen.ShowWarning("No hours entered — timesheet not submitted.");
            ConsoleScreen.Pause();
            return;
        }

        ShowSubmitSummary(weekStart, activeProjects, entries);

        if (!ConsolePrompt.ReadYesNo("Submit this timesheet?"))
        {
            ConsoleScreen.EndScreen();
            return;
        }

        var response = await client.PostAsJsonAsync(
            "/api/employee/timesheets",
            new SubmitTimesheetRequest(weekStart, entries));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var created = await ApiHelper.ReadAsync<TimesheetDetailDto>(response);
            ConsoleScreen.ShowSuccess(created is null
                ? "Timesheet submitted."
                : $"Timesheet submitted for week {created.WeekStartDate:yyyy-MM-dd} with {created.TotalHours:0.##} hours.");
        }

        ConsoleScreen.Pause();
    }

    public static async Task RunHistoryAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("My Timesheets");

        var response = await client.GetAsync("/api/employee/timesheets");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            ConsoleScreen.Pause();
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

        if (timesheets.Count == 0)
        {
            ConsoleScreen.Pause();
            return;
        }

        Console.WriteLine();
        Console.Write("[V] View week detail — enter week start (yyyy-MM-dd), or blank to go back: ");
        var raw = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || ConsolePrompt.WantsToGoBack(raw))
        {
            ConsoleScreen.EndScreen();
            return;
        }

        if (!DateOnly.TryParse(raw, out var weekStart))
        {
            ConsoleScreen.ShowError("Invalid date.");
            ConsoleScreen.Pause();
            return;
        }

        await ShowWeekDetailAsync(client, weekStart);
    }

    private static async Task ShowWeekDetailAsync(HttpClient client, DateOnly weekStart)
    {
        var response = await client.GetAsync($"/api/employee/timesheets/{weekStart:yyyy-MM-dd}");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            ConsoleScreen.Pause();
            return;
        }

        var detail = await ApiHelper.ReadAsync<TimesheetDetailDto>(response);
        if (detail is null)
        {
            ConsoleScreen.Pause();
            return;
        }

        ConsoleScreen.ShowHeader(
            "Timesheet Detail",
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

        ConsoleScreen.Pause();
    }

    private static async Task<List<ActivityTagOptionDto>> LoadActivityTagsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/employee/activity-tags");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return [];
        }

        return await ApiHelper.ReadAsync<List<ActivityTagOptionDto>>(response) ?? [];
    }

    private static List<int> PickActivityTags(IReadOnlyList<ActivityTagOptionDto> tags)
    {
        if (tags.Count == 0)
        {
            return [];
        }

        Console.WriteLine();
        Console.WriteLine("Activity tags (enter comma-separated numbers, blank to skip):");
        ConsoleTable.Print(
            ["#", "Tag ID", "Name", "Category"],
            tags.Select((tag, index) => new[]
            {
                (index + 1).ToString(),
                tag.TagId.ToString(),
                tag.Name,
                tag.Category.ToString()
            }));

        var raw = ConsolePrompt.ReadOptionalText("Tag numbers");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var selected = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var listIndex) && listIndex >= 1 && listIndex <= tags.Count)
            {
                selected.Add(tags[listIndex - 1].TagId);
                continue;
            }

            if (int.TryParse(part, out var tagId) && tags.Any(tag => tag.TagId == tagId))
            {
                selected.Add(tagId);
            }
        }

        return selected.Distinct().ToList();
    }

    private static void ShowSubmitSummary(
        DateOnly weekStart,
        IReadOnlyList<ActiveProjectForTimesheetDto> activeProjects,
        IReadOnlyList<SubmitTimesheetEntryRequest> entries)
    {
        Console.WriteLine();
        Console.WriteLine("── Summary before submit ──────────────────────");
        Console.WriteLine($"Week start: {weekStart:yyyy-MM-dd}");
        Console.WriteLine($"Total hours: {entries.Sum(entry => entry.HoursWorked):0.##}");
        Console.WriteLine();

        ConsoleTable.Print(
            ["Project", "Hours", "Notes", "Tags"],
            entries.Select(entry =>
            {
                var projectName = activeProjects
                    .FirstOrDefault(project => project.ProjectId == entry.ProjectId)
                    ?.ProjectName ?? entry.ProjectId.ToString();

                return new[]
                {
                    projectName,
                    entry.HoursWorked.ToString("0.##"),
                    string.IsNullOrWhiteSpace(entry.Notes) ? "-" : entry.Notes,
                    entry.ActivityTagIds.Count == 0 ? "-" : string.Join(", ", entry.ActivityTagIds)
                };
            }));
    }
}
