using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public interface ILlmCompletionClient
{
    LlmProvider Provider { get; }

    Task<LlmCompletionResult> CompleteAsync(
        LlmCompletionRequest request,
        string apiKey,
        CancellationToken cancellationToken = default);
}
