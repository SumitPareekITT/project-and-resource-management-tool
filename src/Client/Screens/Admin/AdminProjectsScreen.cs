using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Client.Screens.Admin;

/// <summary>
/// Admin workflows for projects and their milestones.
/// </summary>
internal static class AdminProjectsScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        await MenuLoop.RunAsync(
            "Manage Projects",
            null,
            [
                new MenuItem("Create Project", ScreenRunner.Wrap(() => CreateProjectAsync(client))),
                new MenuItem("View All Projects", ScreenRunner.Wrap(() => ViewAllProjectsAsync(client))),
                new MenuItem("Update Project", ScreenRunner.Wrap(() => UpdateProjectAsync(client))),
                new MenuItem("Manage Milestones", () => ManageMilestonesAsync(client)),
            ]);
    }

    private static async Task CreateProjectAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Create Project");

        var name = ConsolePrompt.ReadRequiredText("Project name");
        var clientName = ConsolePrompt.ReadRequiredText("Client name");
        var description = ConsolePrompt.ReadOptionalText("Description");
        var managerId = ConsolePrompt.ReadRequiredInt("Manager user ID");
        var totalStoryPoints = ConsolePrompt.ReadRequiredInt("Total story points");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = ConsolePrompt.ReadDate("Start date", today);
        var endDate = ConsolePrompt.ReadDate("End date", today.AddMonths(6));

        var response = await client.PostAsJsonAsync(
            "/api/projects",
            new CreateProjectRequest(name, clientName, description, startDate, endDate, managerId, totalStoryPoints));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var created = await ApiHelper.ReadAsync<ProjectSummaryDto>(response);
        ConsoleScreen.ShowSuccess(created is null
            ? "Project created."
            : $"Project created: {created.Name} (ID {created.ProjectId})");
    }

    private static async Task ViewAllProjectsAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Projects", "All projects and milestones");

        var response = await client.GetAsync("/api/projects");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var projects = await ApiHelper.ReadAsync<List<ProjectSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Project ID", "Name", "Client", "Status", "Health", "Manager", "SP Done/Total", "Start", "End"],
            projects.Select(project => new[]
            {
                project.ProjectId.ToString(),
                project.Name,
                project.ClientName,
                project.Status.ToString(),
                project.HealthStatus.ToString(),
                project.ManagerName,
                project.StoryPointProgress,
                project.StartDate.ToString("yyyy-MM-dd"),
                project.EndDate.ToString("yyyy-MM-dd")
            }));

        if (projects.Count == 0)
        {
            ConsoleScreen.ShowInfo("No projects found.");
            return;
        }

        Console.WriteLine();
        ConsoleScreen.ShowInfo("Milestones:");
        foreach (var project in projects)
        {
            if (project.Milestones.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"  {project.Name}:");
            PrintMilestones(project.Milestones);
        }
    }

    private static async Task UpdateProjectAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Update Project");

        var projectId = ConsolePrompt.ReadRequiredInt("Project ID");
        var project = await FetchProjectAsync(client, projectId);
        if (project is null)
        {
            return;
        }

        ConsoleScreen.ShowInfo($"Current: {project.Name} | {project.Status} | {project.StoryPointProgress}");

        var name = ReadUpdatedText("Project name", project.Name);
        var clientName = ReadUpdatedText("Client name", project.ClientName);
        var description = ConsolePrompt.ReadOptionalText("Description");
        var managerId = ReadUpdatedInt("Manager user ID", project.ManagerUserId);
        var totalStoryPoints = ReadUpdatedInt("Total story points", project.TotalStoryPoints);
        var completedStoryPoints = ReadUpdatedInt("Completed story points", project.CompletedStoryPoints);
        var startDate = ConsolePrompt.ReadDate("Start date", project.StartDate);
        var endDate = ConsolePrompt.ReadDate("End date", project.EndDate);
        var status = ReadProjectStatus(project.Status);
        if (status is null)
        {
            return;
        }

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}",
            new UpdateProjectRequest(
                name,
                clientName,
                description,
                startDate,
                endDate,
                status.Value,
                managerId,
                totalStoryPoints,
                completedStoryPoints));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var updated = await ApiHelper.ReadAsync<ProjectSummaryDto>(response);
            ConsoleScreen.ShowSuccess(updated is null
                ? "Project updated."
                : $"Project updated: {updated.Name}");
        }
    }

    private static async Task ManageMilestonesAsync(HttpClient client)
    {
        await MenuLoop.RunAsync(
            "Manage Milestones",
            null,
            [
                new MenuItem("Add Milestone", ScreenRunner.Wrap(() => AddMilestoneAsync(client))),
                new MenuItem("Update Milestone Status", ScreenRunner.Wrap(() => UpdateMilestoneStatusAsync(client))),
            ]);
    }

    private static async Task AddMilestoneAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Add Milestone");

        var projectId = ConsolePrompt.ReadRequiredInt("Project ID");
        var title = ConsolePrompt.ReadRequiredText("Title");
        var description = ConsolePrompt.ReadOptionalText("Description");
        var dueDate = ConsolePrompt.ReadDate("Due date");
        var storyPoints = ConsolePrompt.ReadRequiredInt("Story points");
        var completedStoryPoints = ConsolePrompt.ReadOptionalInt("Completed story points") ?? 0;
        var status = ReadMilestoneStatus(MilestoneStatus.NotStarted);
        if (status is null)
        {
            return;
        }

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{projectId}/milestones",
            new UpsertMilestoneRequest(title, description, dueDate, status.Value, storyPoints, completedStoryPoints));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var created = await ApiHelper.ReadAsync<MilestoneDto>(response);
        ConsoleScreen.ShowSuccess(created is null
            ? "Milestone added."
            : $"Milestone added: {created.Title} (ID {created.MilestoneId})");
    }

    private static async Task UpdateMilestoneStatusAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Update Milestone Status");

        var projectId = ConsolePrompt.ReadRequiredInt("Project ID");
        var project = await FetchProjectAsync(client, projectId);
        if (project is null)
        {
            return;
        }

        if (project.Milestones.Count == 0)
        {
            ConsoleScreen.ShowInfo("This project has no milestones.");
            return;
        }

        PrintMilestones(project.Milestones);
        var milestoneId = ConsolePrompt.ReadRequiredInt("Milestone ID");
        var milestone = project.Milestones.FirstOrDefault(item => item.MilestoneId == milestoneId);
        if (milestone is null)
        {
            ConsoleScreen.ShowError("Milestone not found on this project.");
            return;
        }

        var status = ReadMilestoneStatus(milestone.Status);
        if (status is null)
        {
            return;
        }

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/milestones/{milestoneId}/status",
            new UpdateMilestoneStatusRequest(status.Value));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var updated = await ApiHelper.ReadAsync<MilestoneDto>(response);
            ConsoleScreen.ShowSuccess(updated is null
                ? "Milestone status updated."
                : $"Milestone status updated: {updated.Title} is now {updated.Status}");
        }
    }

    private static async Task<ProjectSummaryDto?> FetchProjectAsync(HttpClient client, int projectId)
    {
        var response = await client.GetAsync($"/api/projects/{projectId}");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return null;
        }

        return await ApiHelper.ReadAsync<ProjectSummaryDto>(response);
    }

    private static void PrintMilestones(IReadOnlyList<MilestoneDto> milestones)
    {
        ConsoleTable.Print(
            ["Milestone ID", "Title", "Due Date", "Status", "SP Done/Total"],
            milestones.Select(milestone => new[]
            {
                milestone.MilestoneId.ToString(),
                milestone.Title,
                milestone.DueDate.ToString("yyyy-MM-dd"),
                milestone.Status.ToString(),
                $"{milestone.CompletedStoryPoints}/{milestone.StoryPoints}"
            }));
    }

    private static string ReadUpdatedText(string label, string currentValue)
    {
        Console.Write($"{label} (current: {currentValue}, blank to keep): ");
        var value = Console.ReadLine()?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(value) ? currentValue : value;
    }

    private static int ReadUpdatedInt(string label, int currentValue)
    {
        Console.Write($"{label} (current: {currentValue}, blank to keep): ");
        var raw = Console.ReadLine();
        return int.TryParse(raw, out var parsed) ? parsed : currentValue;
    }

    private static ProjectStatus? ReadProjectStatus(ProjectStatus currentStatus)
    {
        Console.WriteLine("Status options: Planned, Active, OnHold, Completed, Cancelled");
        Console.Write($"Status (current: {currentStatus}, blank to keep): ");
        var raw = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return currentStatus;
        }

        if (Enum.TryParse<ProjectStatus>(raw, ignoreCase: true, out var status))
        {
            return status;
        }

        ConsoleScreen.ShowError("Invalid project status.");
        return null;
    }

    private static MilestoneStatus? ReadMilestoneStatus(MilestoneStatus defaultStatus)
    {
        Console.WriteLine("Status options: NotStarted, InProgress, Completed, Delayed, Blocked");
        Console.Write($"Status (default {defaultStatus}): ");
        var raw = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultStatus;
        }

        if (Enum.TryParse<MilestoneStatus>(raw, ignoreCase: true, out var status))
        {
            return status;
        }

        ConsoleScreen.ShowError("Invalid milestone status.");
        return null;
    }
}
