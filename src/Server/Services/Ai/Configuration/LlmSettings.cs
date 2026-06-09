using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Configuration;

public sealed record LlmSettings(LlmProvider Provider, string ApiKey)
{
    public bool IsConfigured =>
        Provider is not LlmProvider.None
        && !string.IsNullOrWhiteSpace(ApiKey);
}
