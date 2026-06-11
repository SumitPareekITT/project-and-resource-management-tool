using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Client.Screens.Admin;

internal static class AdminEmployeesScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        await MenuLoop.RunAsync(
            "Manage User Profiles",
            null,
            [
                new MenuItem("View All Profiles", ScreenRunner.Wrap(() => ViewAllProfilesAsync(client))),
                new MenuItem("Create Profile", ScreenRunner.Wrap(() => CreateProfileAsync(client))),
                new MenuItem("Update Profile", ScreenRunner.Wrap(() => UpdateProfileAsync(client))),
                new MenuItem("Deactivate Profile", ScreenRunner.Wrap(() => DeactivateProfileAsync(client))),
                new MenuItem("Manage Skills", () => ManageSkillsAsync(client)),
                new MenuItem("Assign Manager", ScreenRunner.Wrap(() => AssignManagerAsync(client))),
            ]);
    }

    private static async Task ViewAllProfilesAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("User Profiles");
        var response = await client.GetAsync("/api/user-profiles");
        if (!await ApiHelper.EnsureSuccessAsync(response)) return;
        var profiles = await ApiHelper.ReadAsync<List<UserProfileSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Profile ID", "User ID", "Name", "Department", "Designation", "Status", "Util %", "Manager", "Active"],
            profiles.Select(p => new[]
            {
                p.ProfileId.ToString(), p.UserId.ToString(), p.FullName, p.Department, p.Designation,
                p.ResourceStatus.ToString(), p.CurrentUtilizationPercent.ToString("0.##"),
                p.ManagerName ?? "-", ApiHelper.YesNo(p.IsActive)
            }));
        if (profiles.Count == 0) ConsoleScreen.ShowInfo("No profiles found.");
    }

    private static async Task CreateProfileAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Create Profile");
        var userId = ConsolePrompt.ReadRequiredInt("User ID");
        var fullName = ConsolePrompt.ReadRequiredText("Full name");
        var email = ConsolePrompt.ReadRequiredText("Email");
        var department = ConsolePrompt.ReadRequiredText("Department");
        var designation = ConsolePrompt.ReadRequiredText("Designation");
        var managerUserId = ConsolePrompt.ReadOptionalInt("Manager user ID");
        var response = await client.PostAsJsonAsync("/api/user-profiles",
            new CreateUserProfileRequest(userId, fullName, email, department, designation, managerUserId));
        if (!await ApiHelper.EnsureSuccessAsync(response)) return;
        var created = await ApiHelper.ReadAsync<UserProfileSummaryDto>(response);
        ConsoleScreen.ShowSuccess(created is null ? "Profile created." : $"Profile created: {created.FullName} (profile {created.ProfileId})");
    }

    private static async Task UpdateProfileAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Update Profile");
        var profileId = ConsolePrompt.ReadRequiredInt("Profile ID");
        var profile = await FetchProfileAsync(client, profileId);
        if (profile is null) return;
        var fullName = ReadUpdatedText("Full name", profile.FullName);
        var email = ReadUpdatedText("Email", profile.Email);
        var department = ReadUpdatedText("Department", profile.Department);
        var designation = ReadUpdatedText("Designation", profile.Designation);
        var managerUserId = ReadUpdatedManagerId(profile.ManagerUserId);
        var response = await client.PutAsJsonAsync($"/api/user-profiles/{profileId}",
            new UpdateUserProfileRequest(fullName, email, department, designation, managerUserId));
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var updated = await ApiHelper.ReadAsync<UserProfileSummaryDto>(response);
            ConsoleScreen.ShowSuccess(updated is null ? "Profile updated." : $"Profile updated: {updated.FullName}");
        }
    }

    private static async Task DeactivateProfileAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Deactivate Profile");
        var profileId = ConsolePrompt.ReadRequiredInt("Profile ID");
        var profile = await FetchProfileAsync(client, profileId);
        if (profile is null) return;
        if (!ConsolePrompt.ReadYesNo($"Deactivate {profile.FullName}?")) return;
        var response = await client.PutAsync($"/api/user-profiles/{profileId}/deactivate", null);
        if (await ApiHelper.EnsureSuccessAsync(response)) ConsoleScreen.ShowSuccess("Profile deactivated.");
    }

    private static async Task ManageSkillsAsync(HttpClient client)
    {
        await MenuLoop.RunAsync("Manage Profile Skills", null, [
            new MenuItem("Add / Update Skill", ScreenRunner.Wrap(() => UpsertSkillAsync(client))),
            new MenuItem("Remove Skill", ScreenRunner.Wrap(() => RemoveSkillAsync(client))),
        ]);
    }

    private static async Task UpsertSkillAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Add / Update Skill");
        var profileId = ConsolePrompt.ReadRequiredInt("Profile ID");
        await ListAvailableSkillsAsync(client);
        var skillId = ConsolePrompt.ReadRequiredInt("Skill ID");
        var proficiency = ReadProficiencyLevel();
        if (proficiency is null) return;
        decimal? years = null;
        Console.Write("Years of experience (blank to skip): ");
        if (decimal.TryParse(Console.ReadLine(), out var parsedYears)) years = parsedYears;
        DateOnly? lastUsed = null;
        Console.Write("Last used yyyy-MM-dd (blank to skip): ");
        var raw = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(raw) && DateOnly.TryParse(raw, out var d)) lastUsed = d;
        var response = await client.PostAsJsonAsync($"/api/user-profiles/{profileId}/skills",
            new UpsertUserSkillRequest(skillId, proficiency.Value, years, lastUsed));
        if (await ApiHelper.EnsureSuccessAsync(response)) ConsoleScreen.ShowSuccess("Skill saved.");
    }

    private static async Task RemoveSkillAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Remove Skill");
        var profileId = ConsolePrompt.ReadRequiredInt("Profile ID");
        var profile = await FetchProfileAsync(client, profileId);
        if (profile is null || profile.Skills.Count == 0) return;
        PrintSkills(profile.Skills);
        var skillId = ConsolePrompt.ReadRequiredInt("Skill ID to remove");
        var response = await client.DeleteAsync($"/api/user-profiles/{profileId}/skills/{skillId}");
        if (await ApiHelper.EnsureSuccessAsync(response)) ConsoleScreen.ShowSuccess("Skill removed.");
    }

    private static async Task AssignManagerAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Assign Manager");
        var userId = ConsolePrompt.ReadRequiredInt("User ID");
        var managerUserId = ConsolePrompt.ReadRequiredInt("Manager user ID");
        var response = await client.PutAsJsonAsync("/api/user-profiles/assign-manager", new AssignManagerRequest(userId, managerUserId));
        if (!await ApiHelper.EnsureSuccessAsync(response)) return;
        var updated = await ApiHelper.ReadAsync<UserProfileSummaryDto>(response);
        ConsoleScreen.ShowSuccess(updated is null ? "Manager assigned." : $"{updated.FullName} -> {updated.ManagerName ?? "manager"}");
    }

    private static async Task<UserProfileSummaryDto?> FetchProfileAsync(HttpClient client, int profileId)
    {
        var response = await client.GetAsync("/api/user-profiles");
        if (!await ApiHelper.EnsureSuccessAsync(response)) return null;
        var profiles = await ApiHelper.ReadAsync<List<UserProfileSummaryDto>>(response) ?? [];
        var profile = profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (profile is null) ConsoleScreen.ShowError("Profile not found in list.");
        return profile;
    }

    private static async Task ListAvailableSkillsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/skills");
        if (!await ApiHelper.EnsureSuccessAsync(response)) return;
        var skills = await ApiHelper.ReadAsync<List<SkillDto>>(response) ?? [];
        ConsoleTable.Print(["Skill ID", "Name", "Category", "Active"],
            skills.Select(s => new[] { s.Id.ToString(), s.Name, s.Category.ToString(), ApiHelper.YesNo(s.IsActive) }));
    }

    private static void PrintSkills(IReadOnlyList<UserSkillDto> skills)
    {
        ConsoleTable.Print(["Skill", "Category", "Proficiency", "Years", "Last Used"],
            skills.Select(s => new[] { s.SkillName, s.Category.ToString(), s.ProficiencyLevel.ToString(),
                s.YearsOfExperience?.ToString("0.#") ?? "-", s.LastUsedOn?.ToString("yyyy-MM-dd") ?? "-" }));
    }

    private static string ReadUpdatedText(string label, string current)
    {
        Console.Write($"{label} (current: {current}, blank to keep): ");
        var value = Console.ReadLine()?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(value) ? current : value;
    }

    private static int? ReadUpdatedManagerId(int? current)
    {
        Console.Write($"Manager user ID (current: {current?.ToString() ?? "none"}, blank=keep, none=clear): ");
        var raw = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return current;
        if (string.Equals(raw, "none", StringComparison.OrdinalIgnoreCase)) return null;
        return int.TryParse(raw, out var id) ? id : current;
    }

    private static ProficiencyLevel? ReadProficiencyLevel()
    {
        Console.Write("Proficiency (Beginner/Intermediate/Advanced/Expert): ");
        return Enum.TryParse<ProficiencyLevel>(Console.ReadLine(), true, out var level) ? level : null;
    }
}
