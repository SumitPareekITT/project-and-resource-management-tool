using System.Net.Http.Json;
using ProjectResourceManagement.Client;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.Enums;

Console.WriteLine("==============================================");
Console.WriteLine("   Project & Resource Management Tool");
Console.WriteLine("==============================================");
Console.Write("Server URL (default http://localhost:5071): ");
var serverUrl = Console.ReadLine();
if (string.IsNullOrWhiteSpace(serverUrl))
{
    serverUrl = "http://localhost:5071";
}

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(serverUrl)
};

var session = await LoginAsync(httpClient);
if (session is null)
{
    return;
}

if (session.ForcePasswordChange)
{
    var changed = await ChangePasswordAsync(httpClient, session);
    if (!changed)
    {
        return;
    }
}

httpClient.DefaultRequestHeaders.Remove("X-User-Role");
httpClient.DefaultRequestHeaders.Add("X-User-Role", session.Role.ToString());

switch (session.Role)
{
    case UserRole.Admin:
        await AdminMenu.RunAsync(httpClient, session);
        break;
    case UserRole.Manager:
        await ManagerMenu.RunAsync(httpClient, session);
        break;
    case UserRole.Employee:
        await EmployeeMenu.RunAsync(httpClient, session);
        break;
    default:
        Console.WriteLine("Unsupported role.");
        break;
}

static async Task<LoginResponse?> LoginAsync(HttpClient client)
{
    Console.WriteLine();
    Console.Write("Username: ");
    var username = Console.ReadLine() ?? string.Empty;
    Console.Write("Password: ");
    var password = Console.ReadLine() ?? string.Empty;

    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
    if (!await ApiHelper.EnsureSuccessAsync(response))
    {
        return null;
    }

    var login = await ApiHelper.ReadAsync<LoginResponse>(response);
    if (login is null)
    {
        Console.WriteLine("Login response was empty.");
        return null;
    }

    Console.WriteLine($"Login successful. Role: {login.Role}");
    return login;
}

static async Task<bool> ChangePasswordAsync(HttpClient client, LoginResponse session)
{
    Console.WriteLine();
    Console.WriteLine("Password change is required before continuing.");
    Console.Write("New password: ");
    var newPassword = Console.ReadLine() ?? string.Empty;
    Console.Write("Confirm password: ");
    var confirmPassword = Console.ReadLine() ?? string.Empty;

    var response = await client.PostAsJsonAsync(
        "/api/auth/change-password",
        new ChangePasswordRequest(session.UserId, newPassword, confirmPassword));

    if (!await ApiHelper.EnsureSuccessAsync(response))
    {
        return false;
    }

    Console.WriteLine("Password changed successfully.");
    return true;
}
