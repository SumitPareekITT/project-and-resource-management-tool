namespace ProjectResourceManagement.Client.Ui;

/// <summary>
/// Console presentation: clear screen, BRD-style headers, messages, and pause between steps.
/// </summary>
internal static class ConsoleScreen
{
    public static void Clear()
    {
        Console.Clear();
    }

    public static void ShowAppBanner()
    {
        Clear();
        Console.WriteLine("==============================================");
        Console.WriteLine("   Project & Resource Management Tool");
        Console.WriteLine("==============================================");
        Console.WriteLine();
    }

    public static void ShowHeader(string title, string? subtitle = null)
    {
        Clear();
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine($"║  {Pad(title)}");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            Console.WriteLine($"║  {Pad(subtitle)}");
        }

        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    public static void ShowInfo(string message)
    {
        Console.WriteLine(message);
    }

    public static void ShowSuccess(string message)
    {
        Console.WriteLine($"{message} ✓");
    }

    public static void ShowWarning(string message)
    {
        Console.WriteLine($"⚠  {message}");
    }

    public static void ShowError(string message)
    {
        Console.WriteLine($"Error: {message}");
    }

    public static void ShowValidationError(string message)
    {
        Console.WriteLine();
        ShowError(message);
        Console.WriteLine();
    }

    public static void ShowBackHint()
    {
        Console.WriteLine();
        Console.WriteLine("[B] Back");
    }

    /// <summary>
    /// Waits for Enter, then clears so the next screen starts fresh.
    /// </summary>
    public static void Pause(string message = "Press Enter to continue...")
    {
        Console.WriteLine();
        Console.Write(message);
        Console.ReadLine();
        Clear();
    }

    /// <summary>
    /// Call at the end of a screen action (view form, API result) before returning to a menu.
    /// </summary>
    public static void EndScreen() => Pause();

    private static string Pad(string text)
    {
        return text.Length >= 44 ? text[..44] : text.PadRight(44);
    }
}
