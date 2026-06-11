namespace ProjectResourceManagement.Client.Ui;

/// <summary>
/// Reusable numbered menu runner used by all role menus.
/// Option 0 means Back (sub-menu) or Logout (top-level menu).
/// </summary>
internal static class MenuLoop
{
    public static async Task RunAsync(
        string title,
        string? subtitle,
        IReadOnlyList<MenuItem> items,
        bool zeroMeansLogout = false)
    {
        while (true)
        {
            ConsoleScreen.ShowHeader(title, subtitle);

            for (var index = 0; index < items.Count; index++)
            {
                Console.WriteLine($" {index + 1}. {items[index].Label}");
            }

            Console.WriteLine(zeroMeansLogout ? " 0. Logout" : " 0. Back");
            Console.Write("Enter option: ");

            var choice = Console.ReadLine()?.Trim();
            if (choice == "0")
            {
                return;
            }

            if (!int.TryParse(choice, out var selected) || selected < 1 || selected > items.Count)
            {
                ConsoleScreen.ShowError("Invalid option.");
                ConsoleScreen.Pause();
                continue;
            }

            await items[selected - 1].Action();
        }
    }
}

internal sealed record MenuItem(string Label, Func<Task> Action);
