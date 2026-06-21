using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Configuration;

public sealed record LlmSettings(
    LlmProvider Provider,
    string ApiKey,
    string GemmaEndpoint = GemmaLlmCompletionClient.DefaultLocalEndpoint,
    string GemmaModel = GemmaLlmCompletionClient.DefaultModelName)
{
    public bool IsConfigured => Provider switch
    {
        LlmProvider.None => false,
        LlmProvider.Gemma => !string.IsNullOrWhiteSpace(ApiKey),
        _ => !string.IsNullOrWhiteSpace(ApiKey)
    };
}
