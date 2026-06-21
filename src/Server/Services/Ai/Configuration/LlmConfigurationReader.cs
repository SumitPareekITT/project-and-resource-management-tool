using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Configuration;

public sealed class LlmConfigurationReader(SystemConfigurationRepository configurationRepository)
{
    public async Task<LlmSettings> ReadAsync(CancellationToken cancellationToken = default)
    {
        var providerConfiguration = await configurationRepository.GetByKeyAsync("LlmProvider", cancellationToken);
        var apiKeyConfiguration = await configurationRepository.GetByKeyAsync("LlmApiKey", cancellationToken);
        var endpointConfiguration = await configurationRepository.GetByKeyAsync("LlmGemmaEndpoint", cancellationToken);
        var modelConfiguration = await configurationRepository.GetByKeyAsync("LlmGemmaModel", cancellationToken);

        var provider = Enum.TryParse<LlmProvider>(providerConfiguration?.Value, ignoreCase: true, out var parsedProvider)
            ? parsedProvider
            : LlmProvider.None;

        var endpoint = endpointConfiguration?.Value?.Trim();
        var model = modelConfiguration?.Value?.Trim();

        return new LlmSettings(
            provider,
            apiKeyConfiguration?.Value?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(endpoint) ? GemmaLlmCompletionClient.DefaultLocalEndpoint : endpoint,
            string.IsNullOrWhiteSpace(model) ? GemmaLlmCompletionClient.DefaultModelName : model);
    }
}
