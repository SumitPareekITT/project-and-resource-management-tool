using System.Net.Http.Json;
using ProjectResourceManagement.Shared.DTOs.Auth;

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
            Console.WriteLine(" 1. Resource dashboard        [Day 6]");
            Console.WriteLine(" 2. Allocate team member     [Day 6]");
            Console.WriteLine(" 3. My projects              [Day 8]");
            Console.WriteLine(" 4. Team timesheets          [Day 7]");
            Console.WriteLine(" 5. AI skill matcher         [Day 9]");
            Console.WriteLine(" 6. Change password");
            Console.WriteLine(" 0. Logout");
            Console.Write("Choose option: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                    Console.WriteLine("This feature will be available in the next implementation phase.");
                    break;
                case "6":
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
