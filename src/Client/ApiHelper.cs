using System.Net.Http.Json;
using System.Text.Json;

namespace ProjectResourceManagement.Client;

internal static class ApiHelper
{
    public static async Task<bool> EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Request failed: {(int)response.StatusCode} {response.StatusCode}");

        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("message", out var message))
                {
                    Console.WriteLine(message.GetString());
                    return false;
                }
            }
            catch
            {
                // Fall through to raw content output.
            }

            Console.WriteLine(content);
        }

        return false;
    }

    public static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(ClientJsonOptions.Instance);
    }

    public static string YesNo(bool value) => value ? "Yes" : "No";
}
