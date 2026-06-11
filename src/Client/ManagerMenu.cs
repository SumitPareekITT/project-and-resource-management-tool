using ProjectResourceManagement.Client.Screens.Employee;
using ProjectResourceManagement.Client.Screens.Manager;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Auth;

namespace ProjectResourceManagement.Client;

/// <summary>
/// Top-level Manager menu — each option delegates to a focused screen class (SRP).
/// </summary>
internal static class ManagerMenu
{
    public static Task RunAsync(HttpClient client, LoginResponse session) =>
        MenuLoop.RunAsync(
            "Manager Menu",
            $"Welcome, {session.FullName}",
            [
                new MenuItem("Resource Dashboard", ScreenRunner.Wrap(() => ManagerDashboardScreen.RunAsync(client))),
                new MenuItem("Allocate Resource", () => ManagerAllocateScreen.RunAsync(client)),
                new MenuItem("My Projects", ScreenRunner.Wrap(() => ManagerProjectsScreen.RunAsync(client))),
                new MenuItem("Timesheets", ScreenRunner.Wrap(() => ManagerTimesheetsScreen.RunAsync(client))),
                new MenuItem("AI Assistant", () => ManagerAiScreen.RunAsync(client))
            ],
            zeroMeansLogout: true);
}
