using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProjectResourceManagement.Client;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.Enums;

ConsoleScreen.ShowAppBanner();
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

while (true)
{
    ConsoleScreen.ShowHeader("Application Start");
    Console.WriteLine(" 1. Login");
    Console.WriteLine(" 2. Exit");
    Console.Write("Enter option: ");

    var startChoice = Console.ReadLine()?.Trim();
    if (startChoice == "2" || string.Equals(startChoice, "exit", StringComparison.OrdinalIgnoreCase))
    {
        ConsoleScreen.Clear();
        break;
    }

    if (startChoice != "1")
    {
        ConsoleScreen.ShowError("Invalid option.");
        ConsoleScreen.Pause();
        continue;
    }

    var session = await LoginAsync(httpClient);
    if (session is null)
    {
        ConsoleScreen.Pause();
        continue;
    }

    ApplyBearerToken(httpClient, session.AccessToken);

    if (session.ForcePasswordChange)
    {
        var changed = await ChangePasswordAsync(httpClient);
        if (!changed)
        {
            ClearBearerToken(httpClient);
            ConsoleScreen.Pause();
            continue;
        }
    }

    switch (ResolvePrimaryRole(session))
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
            ConsoleScreen.ShowError("Unsupported role.");
            ConsoleScreen.Pause();
            break;
    }

    ClearBearerToken(httpClient);
}

static async Task<LoginResponse?> LoginAsync(HttpClient client)
{
    ConsoleScreen.ShowHeader("Login");
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
        ConsoleScreen.ShowError("Login response was empty.");
        return null;
    }

    if (string.IsNullOrWhiteSpace(login.AccessToken))
    {
        ConsoleScreen.ShowError("Login response did not include an access token.");
        return null;
    }

    ConsoleScreen.ShowSuccess($"Login successful. Roles: {string.Join(", ", login.Roles)}");
    ConsoleScreen.Pause();
    return login;
}

static async Task<bool> ChangePasswordAsync(HttpClient client)
{
    ConsoleScreen.ShowHeader("Change Password", "Required before continuing");
    Console.Write("New password: ");
    var newPassword = Console.ReadLine() ?? string.Empty;
    Console.Write("Confirm password: ");
    var confirmPassword = Console.ReadLine() ?? string.Empty;

    var response = await client.PostAsJsonAsync(
        "/api/auth/change-password",
        new ChangePasswordRequest(newPassword, confirmPassword));

    if (!await ApiHelper.EnsureSuccessAsync(response))
    {
        return false;
    }

    ConsoleScreen.ShowSuccess("Password changed successfully.");
    ConsoleScreen.Pause();
    return true;
}

static void ApplyBearerToken(HttpClient client, string accessToken)
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}

static void ClearBearerToken(HttpClient client)
{
    client.DefaultRequestHeaders.Authorization = null;
}

static UserRole ResolvePrimaryRole(LoginResponse session)
{
    if (session.Roles.Any(role => string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)))
    {
        return UserRole.Admin;
    }

    if (session.Roles.Any(role => string.Equals(role, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase)))
    {
        return UserRole.Manager;
    }

    if (session.Roles.Any(role => string.Equals(role, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase)))
    {
        return UserRole.Employee;
    }

    return UserRole.Employee;
}
