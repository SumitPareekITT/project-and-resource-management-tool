using System.Net.Http.Json;
using ProjectResourceManagement.Shared.DTOs.Auth;

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
            Console.WriteLine(" 1. Submit weekly timesheet   [Day 7]");
            Console.WriteLine(" 2. Timesheet history         [Day 7]");
            Console.WriteLine(" 3. My allocations            [Day 6]");
            Console.WriteLine(" 4. Change password");
            Console.WriteLine(" 0. Logout");
            Console.Write("Choose option: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                case "2":
                case "3":
                    Console.WriteLine("This feature will be available in the next implementation phase.");
                    break;
                case "4":
                    await ChangePasswordAsync(client, session);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
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
