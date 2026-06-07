using System.Net.Http.Json;
using System.Text.Json;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

Console.WriteLine("Project & Resource Management Tool");
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
httpClient.DefaultRequestHeaders.Add("X-User-Role", "Admin");

await RunDay4AdminMenuAsync(httpClient);

static async Task RunDay4AdminMenuAsync(HttpClient client)
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Day 4 Admin Test Menu");
        Console.WriteLine("1. Health check");
        Console.WriteLine("2. List skills");
        Console.WriteLine("3. Create skill");
        Console.WriteLine("4. List employees");
        Console.WriteLine("0. Exit");
        Console.Write("Choose option: ");

        var option = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (option)
        {
            case "1":
                await HealthCheckAsync(client);
                break;
            case "2":
                await ListSkillsAsync(client);
                break;
            case "3":
                await CreateSkillAsync(client);
                break;
            case "4":
                await ListEmployeesAsync(client);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }
}

static async Task HealthCheckAsync(HttpClient client)
{
    var response = await client.GetAsync("/health");
    await PrintResponseAsync(response);
}

static async Task ListSkillsAsync(HttpClient client)
{
    var response = await client.GetAsync("/api/skills");
    await PrintResponseAsync(response);
}

static async Task CreateSkillAsync(HttpClient client)
{
    Console.Write("Skill name: ");
    var name = Console.ReadLine() ?? string.Empty;

    Console.WriteLine("Category options: Backend, Frontend, DevOps, QA, Other");
    Console.Write("Category: ");
    var categoryRaw = Console.ReadLine();
    if (!Enum.TryParse<SkillCategory>(categoryRaw, ignoreCase: true, out var category))
    {
        Console.WriteLine("Invalid category.");
        return;
    }

    var payload = new UpsertSkillRequest(name, category);
    var response = await client.PostAsJsonAsync("/api/skills", payload);
    await PrintResponseAsync(response);
}

static async Task ListEmployeesAsync(HttpClient client)
{
    var response = await client.GetAsync("/api/employees");
    await PrintResponseAsync(response);
}

static async Task PrintResponseAsync(HttpResponseMessage response)
{
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");

    if (string.IsNullOrWhiteSpace(content))
    {
        Console.WriteLine("(empty body)");
        return;
    }

    try
    {
        using var document = JsonDocument.Parse(content);
        var pretty = JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(pretty);
    }
    catch
    {
        Console.WriteLine(content);
    }
}
