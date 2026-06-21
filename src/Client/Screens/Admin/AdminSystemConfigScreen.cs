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
                new MenuItem("Configure LLM (provider + API key)", ScreenRunner.Wrap(() => ConfigureLlmAsync(client))),
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
                ["Gemma endpoint", settings.LlmGemmaEndpoint],
                ["Gemma model", settings.LlmGemmaModel],
                ["Scheduler Interval (minutes)", settings.SchedulerIntervalMinutes.ToString()],
                ["Max Weekly Hours", settings.MaxWeeklyHours.ToString()],
            ]);
    }

    private static async Task ConfigureLlmAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Configure LLM", "Provider and API key (console input)");

        Console.WriteLine("Provider options:");
        Console.WriteLine(" 1. None     — use deterministic fallback only");
        Console.WriteLine(" 2. Gemma    — hosted Gemma (default http://164.52.211.238/api/generate)");
        Console.WriteLine(" 3. Gemini   — Google Gemini API");
        Console.WriteLine(" 4. Groq     — Groq cloud API");
        Console.Write("Select provider [1-4]: ");

        var provider = Console.ReadLine()?.Trim() switch
        {
            "1" => "None",
            "2" => "Gemma",
            "3" => "Gemini",
            "4" => "Groq",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(provider))
        {
            ConsoleScreen.ShowError("Invalid provider selection.");
            return;
        }

        if (!await UpdateSettingValueAsync(client, "LlmProvider", provider))
        {
            return;
        }

        if (provider == "None")
        {
            ConsoleScreen.ShowSuccess("LLM disabled. AI features will use deterministic fallback.");
            return;
        }

        if (provider == "Gemma")
        {
            Console.WriteLine();
            Console.WriteLine("Gemma uses POST /api/generate with model gemma3:12b-it-q8_0.");
            Console.WriteLine("Your API key is sent in the 'apikey' request header.");

            var endpoint = ConsolePrompt.ReadOptionalText(
                $"Gemma endpoint (default {GemmaEndpointDefault})");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = GemmaEndpointDefault;
            }

            if (!await UpdateSettingValueAsync(client, "LlmGemmaEndpoint", endpoint))
            {
                return;
            }

            var model = ConsolePrompt.ReadOptionalText(
                $"Gemma model name (default {GemmaModelDefault})");
            if (string.IsNullOrWhiteSpace(model))
            {
                model = GemmaModelDefault;
            }

            if (!await UpdateSettingValueAsync(client, "LlmGemmaModel", model))
            {
                return;
            }

            Console.WriteLine();
            Console.Write("Enter Gemma API key (sent as apikey header): ");
            var apiKey = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ConsoleScreen.ShowError("API key is required for the hosted Gemma server.");
                return;
            }

            if (await UpdateSettingValueAsync(client, "LlmApiKey", apiKey))
            {
                ConsoleScreen.ShowSuccess("Gemma configured. Managers can use AI Skill Match and Risk Summary.");
            }

            return;
        }

        Console.WriteLine();
        Console.Write("Enter API key: ");
        var cloudApiKey = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudApiKey))
        {
            ConsoleScreen.ShowError("API key is required for the selected provider.");
            return;
        }

        if (await UpdateSettingValueAsync(client, "LlmApiKey", cloudApiKey))
        {
            ConsoleScreen.ShowSuccess($"LLM configured: {provider}. Managers can use AI Skill Match and Risk Summary.");
        }
    }

    private const string GemmaEndpointDefault = "http://164.52.211.238/api/generate";
    private const string GemmaModelDefault = "gemma3:12b-it-q8_0";

    private static async Task UpdateSettingAsync(HttpClient client)
    {
        ConsoleScreen.ShowHeader("Update Setting");

        var settings = await FetchSettingsAsync(client);
        if (settings is null)
        {
            return;
        }

        ConsoleScreen.ShowInfo("Allowed keys: LlmProvider, LlmApiKey, LlmGemmaEndpoint, LlmGemmaModel, SchedulerIntervalMinutes, MaxWeeklyHours");
        ConsoleScreen.ShowInfo("LlmProvider values: None, Gemini, Groq, Gemma");

        var key = ConsolePrompt.ReadRequiredText("Setting key");
        var value = key.Equals("LlmApiKey", StringComparison.OrdinalIgnoreCase)
            ? ReadApiKeyFromConsole()
            : ConsolePrompt.ReadRequiredText("New value");

        await UpdateSettingValueAsync(client, key, value);
    }

    private static string ReadApiKeyFromConsole()
    {
        Console.Write("New API key: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static async Task<bool> UpdateSettingValueAsync(HttpClient client, string key, string value)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/system-configuration/{key}",
            new UpdateSystemSettingRequest(value));

        if (!await ApiHelper.EnsureSuccessAsync(response))
        {
            return false;
        }

        var updated = await ApiHelper.ReadAsync<SystemSettingsDto>(response);
        if (updated is not null)
        {
            ConsoleScreen.ShowSuccess($"Setting '{key}' updated.");
            ConsoleScreen.ShowInfo(
                $"LLM Provider: {updated.LlmProvider} | Key: {updated.MaskedLlmApiKey} | Scheduler: {updated.SchedulerIntervalMinutes} min");
        }

        return true;
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
