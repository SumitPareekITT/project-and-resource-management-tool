using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Configuration;

public sealed class LlmConfigurationReader(SystemConfigurationRepository configurationRepository)
{
    public async Task<LlmSettings> ReadAsync(CancellationToken cancellationToken = default)
    {
        var providerConfiguration = await configurationRepository.GetByKeyAsync("LlmProvider", cancellationToken);
        var apiKeyConfiguration = await configurationRepository.GetByKeyAsync("LlmApiKey", cancellationToken);

        var provider = Enum.TryParse<LlmProvider>(providerConfiguration?.Value, ignoreCase: true, out var parsedProvider)
            ? parsedProvider
            : LlmProvider.None;

        return new LlmSettings(provider, apiKeyConfiguration?.Value?.Trim() ?? string.Empty);
    }
}
