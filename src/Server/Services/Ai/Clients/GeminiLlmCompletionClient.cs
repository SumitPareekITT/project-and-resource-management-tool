using System.Net.Http.Json;
using System.Text.Json;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public sealed class GeminiLlmCompletionClient(IHttpClientFactory httpClientFactory) : ILlmCompletionClient
{
    private const string ModelName = "gemini-1.5-flash";

    public LlmProvider Provider => LlmProvider.Gemini;

    public async Task<LlmCompletionResult> CompleteAsync(
        LlmCompletionRequest request,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(GeminiLlmCompletionClient));
        var endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = request.SystemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = request.UserPrompt } }
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return LlmCompletionResult.Failure($"Gemini request failed ({(int)response.StatusCode}): {errorBody}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return string.IsNullOrWhiteSpace(text)
            ? LlmCompletionResult.Failure("Gemini returned an empty response.")
            : LlmCompletionResult.Success(text.Trim());
    }
}
