using System.Net.Http.Json;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Client;

internal static class AdminMenu
{
    public static async Task RunAsync(HttpClient client, LoginResponse session)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"Admin Menu  |  Welcome, {session.FullName}");
            Console.WriteLine("────────────────────────────────────────");
            Console.WriteLine(" 1. List users");
            Console.WriteLine(" 2. Create user");
            Console.WriteLine(" 3. Reset user password");
            Console.WriteLine(" 4. Deactivate user");
            Console.WriteLine(" 5. List skills");
            Console.WriteLine(" 6. Create skill");
            Console.WriteLine(" 7. List employees");
            Console.WriteLine(" 8. List projects");
            Console.WriteLine(" 9. Create project");
            Console.WriteLine("10. Allocation matrix");
            Console.WriteLine("11. Change password");
            Console.WriteLine(" 0. Logout");
            Console.Write("Choose option: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1": await ListUsersAsync(client); break;
                case "2": await CreateUserAsync(client); break;
                case "3": await ResetUserPasswordAsync(client); break;
                case "4": await DeactivateUserAsync(client); break;
                case "5": await ListSkillsAsync(client); break;
                case "6": await CreateSkillAsync(client); break;
                case "7": await ListEmployeesAsync(client); break;
                case "8": await ListProjectsAsync(client); break;
                case "9": await CreateProjectAsync(client); break;
                case "10": await AllocationMatrixAsync(client); break;
                case "11": await ChangePasswordAsync(client, session); break;
                case "0": return;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    private static async Task ListUsersAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/users");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var users = await ApiHelper.ReadAsync<List<UserSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["User ID", "Full Name", "Email", "Username", "Role", "Force PW Change", "Active"],
            users.Select(user => new[]
            {
                user.UserId.ToString(),
                user.FullName,
                user.Email,
                user.Username,
                user.Role.ToString(),
                ApiHelper.YesNo(user.ForcePasswordChange),
                ApiHelper.YesNo(user.IsActive)
            }));
    }

    private static async Task CreateUserAsync(HttpClient client)
    {
        Console.Write("Full name: ");
        var fullName = Console.ReadLine() ?? string.Empty;
        Console.Write("Email: ");
        var email = Console.ReadLine() ?? string.Empty;
        Console.Write("Username: ");
        var username = Console.ReadLine() ?? string.Empty;
        Console.Write("Temporary password: ");
        var password = Console.ReadLine() ?? string.Empty;
        Console.WriteLine("Role options: Manager, Employee");
        Console.Write("Role: ");
        if (!Enum.TryParse<UserRole>(Console.ReadLine(), ignoreCase: true, out var role) || role == UserRole.Admin)
        {
            Console.WriteLine("Invalid role. Choose Manager or Employee.");
            return;
        }

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(fullName, email, username, password, role));
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var created = await ApiHelper.ReadAsync<UserSummaryDto>(response);
        Console.WriteLine(created is null ? "User created." : $"User created: {created.Username} ({created.Role})");
    }

    private static async Task ResetUserPasswordAsync(HttpClient client)
    {
        Console.Write("User ID: ");
        if (!int.TryParse(Console.ReadLine(), out var userId))
        {
            Console.WriteLine("Invalid user ID.");
            return;
        }

        Console.Write("New password: ");
        var password = Console.ReadLine() ?? string.Empty;

        var response = await client.PutAsJsonAsync($"/api/users/{userId}/reset-password", new ResetUserPasswordRequest(password));
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            Console.WriteLine("Password reset successfully. User must change password on next login.");
        }
    }

    private static async Task DeactivateUserAsync(HttpClient client)
    {
        Console.Write("User ID: ");
        if (!int.TryParse(Console.ReadLine(), out var userId))
        {
            Console.WriteLine("Invalid user ID.");
            return;
        }

        var response = await client.PutAsync($"/api/users/{userId}/deactivate", null);
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            Console.WriteLine("User deactivated successfully.");
        }
    }

    private static async Task ListSkillsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/skills");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var skills = await ApiHelper.ReadAsync<List<SkillDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Skill ID", "Name", "Category", "Active"],
            skills.Select(skill => new[]
            {
                skill.Id.ToString(),
                skill.Name,
                skill.Category.ToString(),
                ApiHelper.YesNo(skill.IsActive)
            }));
    }

    private static async Task CreateSkillAsync(HttpClient client)
    {
        Console.Write("Skill name: ");
        var name = Console.ReadLine() ?? string.Empty;
        Console.WriteLine("Category options: Backend, Frontend, DevOps, QA, Other");
        Console.Write("Category: ");
        if (!Enum.TryParse<SkillCategory>(Console.ReadLine(), ignoreCase: true, out var category))
        {
            Console.WriteLine("Invalid category.");
            return;
        }

        var response = await client.PostAsJsonAsync("/api/skills", new UpsertSkillRequest(name, category));
        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var created = await ApiHelper.ReadAsync<SkillDto>(response);
            Console.WriteLine(created is null ? "Skill created." : $"Skill created: {created.Name} ({created.Category})");
        }
    }

    private static async Task ListEmployeesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/employees");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var employees = await ApiHelper.ReadAsync<List<EmployeeSummaryDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Emp ID", "User ID", "Name", "Department", "Designation", "Status", "Util %", "Manager", "Active"],
            employees.Select(employee => new[]
            {
                employee.EmployeeId.ToString(),
                employee.UserId.ToString(),
                employee.FullName,
                employee.Department,
                employee.Designation,
                employee.Status.ToString(),
                employee.CurrentUtilizationPercent.ToString("0.##"),
                employee.ManagerName ?? "-",
                ApiHelper.YesNo(employee.IsActive)
            }));

        if (employees.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Employee skills:");
            foreach (var employee in employees)
            {
                if (employee.Skills.Count == 0)
                {
                    continue;
                }

                Console.WriteLine($"  {employee.FullName}:");
                ConsoleTable.Print(
                    ["Skill", "Category", "Proficiency", "Years Exp", "Last Used"],
                    employee.Skills.Select(skill => new[]
                    {
                        skill.SkillName,
                        skill.Category.ToString(),
                        skill.ProficiencyLevel.ToString(),
                        skill.YearsOfExperience?.ToString("0.#") ?? "-",
                        skill.LastUsedOn?.ToString("yyyy-MM-dd") ?? "-"
                    }));
            }
        }
    }

    private static async Task ListProjectsAsync(HttpClient client)
    {
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

        if (projects.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Milestones:");
            foreach (var project in projects)
            {
                if (project.Milestones.Count == 0)
                {
                    continue;
                }

                Console.WriteLine($"  {project.Name}:");
                ConsoleTable.Print(
                    ["Milestone ID", "Title", "Due Date", "Status", "SP Done/Total"],
                    project.Milestones.Select(milestone => new[]
                    {
                        milestone.MilestoneId.ToString(),
                        milestone.Title,
                        milestone.DueDate.ToString("yyyy-MM-dd"),
                        milestone.Status.ToString(),
                        $"{milestone.CompletedStoryPoints}/{milestone.StoryPoints}"
                    }));
            }
        }
    }

    private static async Task CreateProjectAsync(HttpClient client)
    {
        Console.Write("Project name: ");
        var name = Console.ReadLine() ?? string.Empty;
        Console.Write("Client name: ");
        var clientName = Console.ReadLine() ?? string.Empty;
        Console.Write("Description: ");
        var description = Console.ReadLine() ?? string.Empty;
        Console.Write("Manager user ID: ");
        if (!int.TryParse(Console.ReadLine(), out var managerId))
        {
            Console.WriteLine("Invalid manager ID.");
            return;
        }

        Console.Write("Total story points: ");
        if (!int.TryParse(Console.ReadLine(), out var totalStoryPoints))
        {
            Console.WriteLine("Invalid story points.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.PostAsJsonAsync(
            "/api/projects",
            new CreateProjectRequest(name, clientName, description, today, today.AddMonths(6), managerId, totalStoryPoints));

        if (await ApiHelper.EnsureSuccessAsync(response))
        {
            var created = await ApiHelper.ReadAsync<ProjectSummaryDto>(response);
            Console.WriteLine(created is null ? "Project created." : $"Project created: {created.Name} (ID {created.ProjectId})");
        }
    }

    private static async Task AllocationMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/allocations/matrix");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var rows = await ApiHelper.ReadAsync<List<AllocationMatrixRowDto>>(response) ?? [];
        ConsoleTable.Print(
            ["Alloc ID", "Employee", "Project", "Manager", "Util %", "From", "To", "Status"],
            rows.Select(row => new[]
            {
                row.AllocationId.ToString(),
                row.EmployeeName,
                row.ProjectName,
                row.ManagerName,
                row.UtilizationPercentage.ToString("0.##"),
                row.FromDate.ToString("yyyy-MM-dd"),
                row.ToDate?.ToString("yyyy-MM-dd") ?? "-",
                row.Status
            }));
    }

    private static async Task ChangePasswordAsync(HttpClient client, LoginResponse session)
    {
        Console.Write("Current/new password: ");
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
