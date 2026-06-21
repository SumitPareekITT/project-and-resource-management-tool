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
                new MenuItem("Manage Employee Skills", ScreenRunner.Wrap(() => ManageSkillsAsync(client))),
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
        ConsoleScreen.ShowHeader("Manage Employee Skills");

        var userId = ConsolePrompt.ReadRequiredInt("Enter Employee User ID");
        var profile = await FetchProfileByUserIdAsync(client, userId);
        if (profile is null)
        {
            ConsoleScreen.ShowError("No employee profile found for that user ID. Create a profile first.");
            return;
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"── {profile.FullName} (User ID {profile.UserId}) ──");
            if (profile.Skills.Count == 0)
            {
                Console.WriteLine("Current Skills: (none yet)");
            }
            else
            {
                Console.WriteLine("Current Skills:");
                for (var index = 0; index < profile.Skills.Count; index++)
                {
                    var skill = profile.Skills[index];
                    Console.WriteLine($"  {index + 1}.  {skill.SkillName,-22} {skill.ProficiencyLevel}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("1. Add Skill");
            Console.WriteLine("2. Update Proficiency Level");
            Console.WriteLine("3. Remove Skill");
            Console.WriteLine("4. Back");
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    profile = await AddSkillAsync(client, profile) ?? profile;
                    break;
                case "2":
                    profile = await UpdateSkillProficiencyAsync(client, profile) ?? profile;
                    break;
                case "3":
                    profile = await RemoveSkillFromProfileAsync(client, profile) ?? profile;
                    break;
                case "4":
                    return;
                default:
                    ConsoleScreen.ShowError("Invalid option.");
                    break;
            }
        }
    }

    private static async Task<UserProfileSummaryDto?> AddSkillAsync(HttpClient client, UserProfileSummaryDto profile)
    {
        Console.WriteLine();
        var skillName = ConsolePrompt.ReadRequiredText("Skill name");
        var category = ReadSkillCategory();
        if (category is null)
        {
            ConsoleScreen.ShowError("Invalid category choice.");
            return null;
        }

        var proficiency = ReadRequiredProficiencyLevel();
        if (proficiency is null)
        {
            ConsoleScreen.ShowError("Invalid proficiency choice.");
            return null;
        }

        var response = await client.PostAsJsonAsync(
            $"/api/user-profiles/by-user/{profile.UserId}/skills",
            new AddUserSkillByNameRequest(skillName, category.Value, proficiency.Value));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return null;
        }

        ConsoleScreen.ShowSuccess("Skill added.");
        return await FetchProfileByUserIdAsync(client, profile.UserId);
    }

    private static async Task<UserProfileSummaryDto?> UpdateSkillProficiencyAsync(HttpClient client, UserProfileSummaryDto profile)
    {
        if (profile.Skills.Count == 0)
        {
            ConsoleScreen.ShowInfo("This employee has no skills to update.");
            return profile;
        }

        Console.WriteLine();
        PrintSkills(profile.Skills);
        Console.Write("Enter skill number to update: ");
        if (!int.TryParse(Console.ReadLine(), out var selected) || selected < 1 || selected > profile.Skills.Count)
        {
            ConsoleScreen.ShowError("Invalid skill number.");
            return profile;
        }

        var skill = profile.Skills[selected - 1];
        var proficiency = ReadRequiredProficiencyLevel();
        if (proficiency is null)
        {
            return profile;
        }

        var response = await client.PostAsJsonAsync(
            $"/api/user-profiles/{profile.ProfileId}/skills",
            new UpsertUserSkillRequest(skill.SkillId, proficiency.Value, skill.YearsOfExperience, skill.LastUsedOn));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return profile;
        }

        ConsoleScreen.ShowSuccess("Proficiency updated.");
        return await FetchProfileByUserIdAsync(client, profile.UserId);
    }

    private static async Task<UserProfileSummaryDto?> RemoveSkillFromProfileAsync(HttpClient client, UserProfileSummaryDto profile)
    {
        if (profile.Skills.Count == 0)
        {
            ConsoleScreen.ShowInfo("This employee has no skills to remove.");
            return profile;
        }

        Console.WriteLine();
        PrintSkills(profile.Skills);
        Console.Write("Enter skill number to remove: ");
        if (!int.TryParse(Console.ReadLine(), out var selected) || selected < 1 || selected > profile.Skills.Count)
        {
            ConsoleScreen.ShowError("Invalid skill number.");
            return profile;
        }

        var skill = profile.Skills[selected - 1];
        if (!ConsolePrompt.ReadYesNo($"Remove {skill.SkillName} from {profile.FullName}?"))
        {
            return profile;
        }

        var response = await client.DeleteAsync($"/api/user-profiles/{profile.ProfileId}/skills/{skill.SkillId}");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return profile;
        }

        ConsoleScreen.ShowSuccess("Skill removed.");
        return await FetchProfileByUserIdAsync(client, profile.UserId);
    }

    private static async Task<UserProfileSummaryDto?> FetchProfileByUserIdAsync(HttpClient client, int userId)
    {
        var response = await client.GetAsync("/api/user-profiles");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return null;
        }

        var profiles = await ApiHelper.ReadAsync<List<UserProfileSummaryDto>>(response) ?? [];
        return profiles.FirstOrDefault(p => p.UserId == userId);
    }

    private static SkillCategory? ReadSkillCategory()
    {
        Console.WriteLine("Category: (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
        Console.Write("Enter choice: ");
        return Console.ReadLine()?.Trim() switch
        {
            "1" => SkillCategory.Backend,
            "2" => SkillCategory.Frontend,
            "3" => SkillCategory.DevOps,
            "4" => SkillCategory.QA,
            "5" => SkillCategory.Other,
            _ => null
        };
    }

    private static ProficiencyLevel? ReadRequiredProficiencyLevel()
    {
        Console.WriteLine("Proficiency: (1) Beginner  (2) Intermediate  (3) Advanced  (4) Expert");
        Console.Write("Enter choice: ");
        return Console.ReadLine()?.Trim() switch
        {
            "1" => ProficiencyLevel.Beginner,
            "2" => ProficiencyLevel.Intermediate,
            "3" => ProficiencyLevel.Advanced,
            "4" => ProficiencyLevel.Expert,
            _ => null
        };
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

    private static void PrintSkills(IReadOnlyList<UserSkillDto> skills)
    {
        ConsoleTable.Print(["#", "Skill", "Category", "Proficiency", "Years", "Last Used"],
            skills.Select((s, index) => new[]
            {
                (index + 1).ToString(),
                s.SkillName,
                s.Category.ToString(),
                s.ProficiencyLevel.ToString(),
                s.YearsOfExperience?.ToString("0.#") ?? "-",
                s.LastUsedOn?.ToString("yyyy-MM-dd") ?? "-"
            }));
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
}
