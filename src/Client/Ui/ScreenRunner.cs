namespace ProjectResourceManagement.Client.Ui;

/// <summary>
/// Wraps a screen action so it always pauses and clears before returning to a menu.
/// </summary>
internal static class ScreenRunner
{
    public static Func<Task> Wrap(Func<Task> action) => async () =>
    {
        await action();
        ConsoleScreen.EndScreen();
    };
}
