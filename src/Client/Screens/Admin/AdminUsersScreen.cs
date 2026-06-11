using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Client.Screens.Admin;

/// <summary>
/// Admin workflows for user accounts (create, view, password reset, activate/deactivate).
/// </summary>
internal static class AdminUsersScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        await MenuLoop.RunAsync(
            "Manage Users",
            null,
            [
                new MenuItem("Create User", ScreenRunner.Wrap(() => CreateUserAsync(client))),
                new MenuItem("View All Users", ScreenRunner.Wrap(() => ViewAllUsersAsync(client))),
                new MenuItem("Reset Password", ScreenRunner.Wrap(() => ResetPasswordAsync(client))),
                new MenuItem("Deactivate User", ScreenRunner.Wrap(() => DeactivateUserAsync(client))),
                new MenuItem("Reactivate User", ScreenRunner.Wrap(() => ReactivateUserAsync(client))),
            ]);
    }

    private static async Task CreateUserAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Create User");

        var fullName = ConsolePrompt.ReadRequiredText("Full name");
        var email = ConsolePrompt.ReadRequiredText("Email");
        var username = ConsolePrompt.ReadRequiredText("Username");
        var password = ConsolePrompt.ReadRequiredText("Temporary password");
        var role = ReadUserRole();
        if (role is null)
        {
            return;
        }

        var department = ConsolePrompt.ReadRequiredText("Department");
        var designation = ConsolePrompt.ReadRequiredText("Designation");
        int? managerUserId = null;
        if (role == UserRole.Employee)
        {
            managerUserId = ConsolePrompt.ReadOptionalInt("Manager user ID");
        }

        var response = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(fullName, email, username, password, role.Value, department, designation, managerUserId));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var created = await ApiHelper.ReadAsync<UserSummaryDto>(response);
        ConsoleScreen.ShowSuccess(created is null
            ? "User created."
            : "User created: " + created.Username + " (" + string.Join(", ", created.Roles) + ")");
    }

    private static async Task ViewAllUsersAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Users", "All user accounts");

        var response = await client.GetAsync("/api/users");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var users = await ApiHelper.ReadAsync<List<UserSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["User ID", "Full Name", "Email", "Username", "Roles", "Force PW Change", "Active"],
            users.Select(user => new[]
            {
                user.UserId.ToString(),
                user.FullName,
                user.Email,
                user.Username,
                string.Join(", ", user.Roles),
                ApiHelper.YesNo(user.ForcePasswordChange),
                ApiHelper.YesNo(user.IsActive)
            }));

        if (users.Count == 0)
        {
            ConsoleScreen.ShowInfo("No users found.");
        }
    }

    private static async Task ResetPasswordAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Reset Password");

        var userId = ConsolePrompt.ReadRequiredInt("User ID");
        var password = ConsolePrompt.ReadRequiredText("New password");

        var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/reset-password",
            new ResetUserPasswordRequest(password));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            ConsoleScreen.ShowSuccess("Password reset. User must change password on next login.");
        }
    }

    private static async Task DeactivateUserAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Deactivate User");

        var userId = ConsolePrompt.ReadRequiredInt("User ID");
        ConsoleScreen.ShowWarning("Deactivated users cannot log in until reactivated.");
        if (!ConsolePrompt.ReadYesNo("Are you sure you want to continue?"))
        {
            ConsoleScreen.ShowInfo("Deactivation cancelled.");
            return;
        }

        var response = await client.PutAsync($"/api/users/{userId}/deactivate", null);
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            ConsoleScreen.ShowSuccess("User deactivated successfully.");
        }
    }

    private static async Task ReactivateUserAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Reactivate User");

        var userId = ConsolePrompt.ReadRequiredInt("User ID");
        var response = await client.PutAsync($"/api/users/{userId}/reactivate", null);
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var reactivated = await ApiHelper.ReadAsync<UserSummaryDto>(response);
        ConsoleScreen.ShowSuccess(reactivated is null
            ? "User reactivated."
            : $"User reactivated: {reactivated.Username}");
    }

    private static UserRole? ReadUserRole()
    {
        Console.WriteLine("Role options: Manager, Employee");
        Console.Write("Role: ");
        if (!Enum.TryParse<UserRole>(Console.ReadLine(), ignoreCase: true, out var role) || role == UserRole.Admin)
        {
            ConsoleScreen.ShowError("Invalid role. Choose Manager or Employee.");
            return null;
        }

        return role;
    }
}
