using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public sealed class GroqLlmCompletionClient(IHttpClientFactory httpClientFactory) : ILlmCompletionClient
{
    private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const string ModelName = "llama-3.1-8b-instant";

    public LlmProvider Provider => LlmProvider.Groq;

    public async Task<LlmCompletionResult> CompleteAsync(
        LlmCompletionRequest request,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(GroqLlmCompletionClient));
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(new
        {
            model = ModelName,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = request.SystemInstruction },
                new { role = "user", content = request.UserPrompt }
            }
        });

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return LlmCompletionResult.Failure($"Groq request failed ({(int)response.StatusCode}): {errorBody}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var text = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return string.IsNullOrWhiteSpace(text)
            ? LlmCompletionResult.Failure("Groq returned an empty response.")
            : LlmCompletionResult.Success(text.Trim());
    }
}
