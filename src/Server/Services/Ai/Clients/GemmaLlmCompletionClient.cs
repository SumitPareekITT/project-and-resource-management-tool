using System.Net.Http.Json;
using System.Text.Json;
using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Clients;

/// <summary>
/// Adapter for a self-hosted Gemma endpoint (Ollama-style POST /api/generate).
/// Sends the API key in the <c>apikey</c> request header, matching the hosted Gemma server contract.
/// </summary>
public sealed class GemmaLlmCompletionClient(IHttpClientFactory httpClientFactory) : ILlmCompletionClient
{
    public const string ApiKeyHeaderName = "apikey";
    public const string DefaultEndpoint = "http://164.52.211.238/api/generate";
    public const string DefaultModelName = "gemma3:12b-it-q8_0";

    /// <summary>Backward-compatible alias used by configuration defaults.</summary>
    public const string DefaultLocalEndpoint = DefaultEndpoint;

    public LlmProvider Provider => LlmProvider.Gemma;

    public async Task<LlmCompletionResult> CompleteAsync(
        LlmCompletionRequest request,
        LlmSettings settings,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(GemmaLlmCompletionClient));
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, settings.GemmaEndpoint);

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            httpRequest.Headers.TryAddWithoutValidation(ApiKeyHeaderName, settings.ApiKey);
        }

        var prompt = $"{request.SystemInstruction}\n\n{request.UserPrompt}";
        httpRequest.Content = JsonContent.Create(new
        {
            model = settings.GemmaModel,
            prompt,
            stream = false
        });

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return LlmCompletionResult.Failure($"Gemma request failed ({(int)response.StatusCode}): {errorBody}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var text = TryReadResponseText(document.RootElement);
            return string.IsNullOrWhiteSpace(text)
                ? LlmCompletionResult.Failure("Gemma returned an empty response.")
                : LlmCompletionResult.Success(text.Trim());
        }
        catch (HttpRequestException exception)
        {
            return LlmCompletionResult.Failure(
                $"Cannot reach Gemma endpoint ({settings.GemmaEndpoint}). " +
                $"Check the server is up and LlmGemmaEndpoint is correct in Admin settings. {exception.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return LlmCompletionResult.Failure(
                $"Gemma request timed out ({settings.GemmaEndpoint}). The model may still be loading.");
        }
    }

    public static string? TryReadResponseText(JsonElement root)
    {
        if (root.TryGetProperty("response", out var responseElement))
        {
            return responseElement.GetString();
        }

        if (root.TryGetProperty("text", out var textElement))
        {
            return textElement.GetString();
        }

        if (root.TryGetProperty("choices", out var choicesElement)
            && choicesElement.GetArrayLength() > 0)
        {
            var firstChoice = choicesElement[0];
            if (firstChoice.TryGetProperty("message", out var messageElement)
                && messageElement.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString();
            }
        }

        return null;
    }
}
