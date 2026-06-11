using System.Net.Http.Json;
using ProjectResourceManagement.Client.Ui;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Client.Screens.Admin;

/// <summary>
/// Admin screen for viewing and updating runtime system configuration settings.
/// </summary>
internal static class AdminSystemConfigScreen
{
    public static async Task RunAsync(HttpClient client)
    {
        await MenuLoop.RunAsync(
            "System Configuration",
            null,
            [
                new MenuItem("View Settings", ScreenRunner.Wrap(() => ViewSettingsAsync(client))),
                new MenuItem("Update Setting", ScreenRunner.Wrap(() => UpdateSettingAsync(client))),
            ]);
    }

    private static async Task ViewSettingsAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("System Settings");

        var settings = await FetchSettingsAsync(client);
        if (settings is null)
        {
            return;
        }

        ConsoleTable.Print(
            ["Setting", "Value"],
            [
                ["LLM Provider", settings.LlmProvider],
                ["LLM API Key", settings.MaskedLlmApiKey],
                ["Scheduler Interval (minutes)", settings.SchedulerIntervalMinutes.ToString()],
                ["Max Weekly Hours", settings.MaxWeeklyHours.ToString()],
            ]);
    }

    private static async Task UpdateSettingAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Update Setting");

        var settings = await FetchSettingsAsync(client);
        if (settings is null)
        {
            return;
        }

        ConsoleScreen.ShowInfo("Allowed keys: LlmProvider, LlmApiKey, SchedulerIntervalMinutes, MaxWeeklyHours");
        ConsoleScreen.ShowInfo("LlmProvider values: None, Gemini, Groq");

        var key = ConsolePrompt.ReadRequiredText("Setting key");
        var value = ConsolePrompt.ReadRequiredText("New value");

        var response = await client.PutAsJsonAsync(
            $"/api/system-configuration/{key}",
            new UpdateSystemSettingRequest(value));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return;
        }

        var updated = await ApiHelper.ReadAsync<SystemSettingsDto>(response);
        if (updated is not null)
        {
            ConsoleScreen.ShowSuccess($"Setting '{key}' updated.");
            ConsoleScreen.ShowInfo($"LLM Provider: {updated.LlmProvider} | Scheduler: {updated.SchedulerIntervalMinutes} min | Max hours: {updated.MaxWeeklyHours}");
        }
    }

    private static async Task<SystemSettingsDto?> FetchSettingsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/system-configuration");
        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return null;
        }

        return await ApiHelper.ReadAsync<SystemSettingsDto>(response);
    }
}
