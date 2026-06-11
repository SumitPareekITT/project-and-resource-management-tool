using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Server.Services.Ai.Fallback;
using ProjectResourceManagement.Server.Services.Ai.Prompts;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai;

public sealed class AiAssistantService(
    UserProfileRepository userProfileRepository,
    ProjectRepository projectRepository,
    SkillMatchCandidateFilter skillMatchCandidateFilter,
    ProjectRiskFactAssembler projectRiskFactAssembler,
    SkillMatchPromptBuilder skillMatchPromptBuilder,
    ProjectRiskPromptBuilder projectRiskPromptBuilder,
    DeterministicSkillMatchSummarizer deterministicSkillMatchSummarizer,
    DeterministicProjectRiskSummarizer deterministicProjectRiskSummarizer,
    LlmConfigurationReader llmConfigurationReader,
    LlmCompletionClientFactory llmCompletionClientFactory)
{
    public async Task<AdminResult<AiSkillMatchResponse>> MatchSkillsAsync(
        int managerUserId,
        AiSkillMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return AdminResult<AiSkillMatchResponse>.Fail(
                AdminResultCode.ValidationError,
                "Query is required for AI skill matching.");
        }

        if (request.ProjectId is int projectId)
        {
            var ownedProject = await projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (ownedProject is null)
            {
                return AdminResult<AiSkillMatchResponse>.Fail(AdminResultCode.NotFound, "Project was not found.");
            }

            if (ownedProject.ManagerUserId != managerUserId)
            {
                return AdminResult<AiSkillMatchResponse>.Fail(
                    AdminResultCode.ValidationError,
                    "You can run skill matching only for projects you own.");
            }
        }

        var directTeam = await userProfileRepository.ListByManagerUserIdAsync(managerUserId, cancellationToken);
        var filteredCandidates = skillMatchCandidateFilter.FilterDirectTeam(directTeam, request.Query.Trim());
        var llmSettings = await llmConfigurationReader.ReadAsync(cancellationToken);
        var summary = await BuildSkillMatchSummaryAsync(request.Query.Trim(), filteredCandidates, llmSettings, cancellationToken);

        var response = new AiSkillMatchResponse(
            request.Query.Trim(),
            filteredCandidates.Select(MapSkillMatchCandidate).ToList(),
            summary.Text,
            summary.UsedFallback,
            summary.ProviderUsed);

        return AdminResult<AiSkillMatchResponse>.Success(response);
    }

    public async Task<AdminResult<AiProjectRiskSummaryResponse>> SummarizeProjectRiskAsync(
        int managerUserId,
        AiProjectRiskSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var facts = await projectRiskFactAssembler.AssembleOwnedProjectFactsAsync(
            managerUserId,
            request.ProjectId,
            cancellationToken);

        if (facts is null)
        {
            return AdminResult<AiProjectRiskSummaryResponse>.Fail(
                AdminResultCode.NotFound,
                "Project was not found or is not owned by this manager.");
        }

        var factLines = deterministicProjectRiskSummarizer.ToFactLines(facts);
        var llmSettings = await llmConfigurationReader.ReadAsync(cancellationToken);
        var summary = await BuildProjectRiskSummaryAsync(facts, llmSettings, cancellationToken);

        var response = new AiProjectRiskSummaryResponse(
            facts.ProjectId,
            facts.ProjectName,
            facts.HealthStatus,
            factLines,
            summary.Text,
            summary.UsedFallback,
            summary.ProviderUsed);

        return AdminResult<AiProjectRiskSummaryResponse>.Success(response);
    }

    private async Task<SummaryResult> BuildSkillMatchSummaryAsync(
        string query,
        IReadOnlyList<SkillMatchCandidate> candidates,
        LlmSettings llmSettings,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return new SummaryResult(
                deterministicSkillMatchSummarizer.Summarize(query, candidates),
                UsedFallback: true,
                ProviderUsed: LlmProvider.None);
        }

        var llmClient = llmCompletionClientFactory.Resolve(llmSettings);
        if (llmClient is null)
        {
            return new SummaryResult(
                deterministicSkillMatchSummarizer.Summarize(query, candidates),
                UsedFallback: true,
                ProviderUsed: LlmProvider.None);
        }

        var prompt = skillMatchPromptBuilder.Build(query, candidates);
        var completion = await llmClient.CompleteAsync(prompt, llmSettings.ApiKey, cancellationToken);
        if (!completion.Succeeded)
        {
            return new SummaryResult(
                $"{deterministicSkillMatchSummarizer.Summarize(query, candidates)} LLM error: {completion.ErrorMessage}",
                UsedFallback: true,
                ProviderUsed: llmSettings.Provider);
        }

        return new SummaryResult(completion.Content, UsedFallback: false, ProviderUsed: llmSettings.Provider);
    }

    private async Task<SummaryResult> BuildProjectRiskSummaryAsync(
        ProjectRiskFacts facts,
        LlmSettings llmSettings,
        CancellationToken cancellationToken)
    {
        var llmClient = llmCompletionClientFactory.Resolve(llmSettings);
        if (llmClient is null)
        {
            return new SummaryResult(
                deterministicProjectRiskSummarizer.Summarize(facts),
                UsedFallback: true,
                ProviderUsed: LlmProvider.None);
        }

        var prompt = projectRiskPromptBuilder.Build(facts);
        var completion = await llmClient.CompleteAsync(prompt, llmSettings.ApiKey, cancellationToken);
        if (!completion.Succeeded)
        {
            return new SummaryResult(
                $"{deterministicProjectRiskSummarizer.Summarize(facts)} LLM error: {completion.ErrorMessage}",
                UsedFallback: true,
                ProviderUsed: llmSettings.Provider);
        }

        return new SummaryResult(completion.Content, UsedFallback: false, ProviderUsed: llmSettings.Provider);
    }

    private static SkillMatchCandidateDto MapSkillMatchCandidate(SkillMatchCandidate candidate)
    {
        return new SkillMatchCandidateDto(
            candidate.Profile.UserId,
            candidate.Profile.FullName,
            candidate.Profile.Department,
            candidate.Profile.Designation,
            candidate.Status,
            candidate.CurrentUtilizationPercent,
            candidate.MatchScore,
            candidate.MatchedSkills,
            candidate.DeterministicExplanation);
    }

    private sealed record SummaryResult(string Text, bool UsedFallback, LlmProvider ProviderUsed);
}