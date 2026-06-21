using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Server.Services.Ai.Fallback;
using ProjectResourceManagement.Server.Services.Ai.Prompts;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai;

public sealed class ProjectRiskSummaryService(
    DeterministicProjectRiskSummarizer deterministicProjectRiskSummarizer,
    ProjectRiskPromptBuilder projectRiskPromptBuilder,
    LlmConfigurationReader llmConfigurationReader,
    LlmCompletionClientFactory llmCompletionClientFactory)
{
    public async Task<ProjectRiskSummaryResult> SummarizeAsync(
        ProjectRiskFacts facts,
        CancellationToken cancellationToken = default)
    {
        var llmSettings = await llmConfigurationReader.ReadAsync(cancellationToken);
        var llmClient = llmCompletionClientFactory.Resolve(llmSettings);
        if (llmClient is null)
        {
            return new ProjectRiskSummaryResult(
                $"{deterministicProjectRiskSummarizer.Summarize(facts)} LLM provider is not configured, so this summary uses system facts only.",
                UsedFallback: true,
                ProviderUsed: LlmProvider.None);
        }

        var prompt = projectRiskPromptBuilder.Build(facts);
        var completion = await llmClient.CompleteAsync(prompt, llmSettings, cancellationToken);
        if (!completion.Succeeded)
        {
            return new ProjectRiskSummaryResult(
                $"{deterministicProjectRiskSummarizer.Summarize(facts)} LLM error: {completion.ErrorMessage}",
                UsedFallback: true,
                ProviderUsed: llmSettings.Provider);
        }

        return new ProjectRiskSummaryResult(completion.Content, UsedFallback: false, ProviderUsed: llmSettings.Provider);
    }
}

public sealed record ProjectRiskSummaryResult(string Text, bool UsedFallback, LlmProvider ProviderUsed);
