using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public sealed class LlmCompletionClientFactory(IEnumerable<ILlmCompletionClient> clients)
{
    public ILlmCompletionClient? Resolve(LlmSettings settings)
    {
        if (!settings.IsConfigured)
        {
            return null;
        }

        return clients.FirstOrDefault(client => client.Provider == settings.Provider);
    }
}
