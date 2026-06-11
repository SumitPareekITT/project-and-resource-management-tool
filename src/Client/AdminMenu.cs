using ProjectResourceManagement.Client.Screens.Admin;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Auth;

namespace ProjectResourceManagement.Client;

/// <summary>
/// BRD-aligned top-level Admin console menu. Each option delegates to a dedicated screen class.
/// </summary>
internal static class AdminMenu
{
    public static async Task RunAsync(HttpClient client, LoginResponse session)
    {
        await MenuLoop.RunAsync(
            "Admin Console",
            $"Welcome, {session.FullName}",
            [
                new MenuItem("Manage Employees", () => AdminEmployeesScreen.RunAsync(client)),
                new MenuItem("Manage Projects", () => AdminProjectsScreen.RunAsync(client)),
                new MenuItem("View All Allocations", ScreenRunner.Wrap(() => AdminAllocationsScreen.RunAsync(client))),
                new MenuItem("Manage Users", () => AdminUsersScreen.RunAsync(client)),
                new MenuItem("System Configuration", () => AdminSystemConfigScreen.RunAsync(client)),
            ],
            zeroMeansLogout: true);
    }
}
