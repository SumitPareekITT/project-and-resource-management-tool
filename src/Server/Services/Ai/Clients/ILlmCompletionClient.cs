using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public interface ILlmCompletionClient
{
    LlmProvider Provider { get; }

    Task<LlmCompletionResult> CompleteAsync(
        LlmCompletionRequest request,
        LlmSettings settings,
        CancellationToken cancellationToken = default);
}
