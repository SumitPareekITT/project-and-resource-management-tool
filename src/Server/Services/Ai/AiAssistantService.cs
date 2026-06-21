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
    AllocationRepository allocationRepository,
    SkillMatchCandidateFilter skillMatchCandidateFilter,
    OrganizationTeamMatcher organizationTeamMatcher,
    ProjectRiskFactAssembler projectRiskFactAssembler,
    SkillMatchPromptBuilder skillMatchPromptBuilder,
    ProjectRiskSummaryService projectRiskSummaryService,
    TeamMatchPromptBuilder teamMatchPromptBuilder,
    DeterministicSkillMatchSummarizer deterministicSkillMatchSummarizer,
    DeterministicProjectRiskSummarizer deterministicProjectRiskSummarizer,
    DeterministicTeamMatchSummarizer deterministicTeamMatchSummarizer,
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
        var employeeTeam = directTeam
            .Where(profile => profile.User.RoleAssignments.Any(assignment =>
                assignment.Role.RoleName.Equals(nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var filteredCandidates = skillMatchCandidateFilter.FilterDirectTeam(employeeTeam, request.Query.Trim());
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

    public async Task<AdminResult<AiTeamMatchResponse>> MatchOrganizationTeamAsync(
        int managerUserId,
        AiTeamMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Roles is null || request.Roles.Count == 0)
        {
            return AdminResult<AiTeamMatchResponse>.Fail(
                AdminResultCode.ValidationError,
                "At least one team role requirement is required.");
        }

        foreach (var role in request.Roles)
        {
            if (string.IsNullOrWhiteSpace(role.RoleTitle) || string.IsNullOrWhiteSpace(role.RequiredSkillName))
            {
                return AdminResult<AiTeamMatchResponse>.Fail(
                    AdminResultCode.ValidationError,
                    "Each role must include a role title and required skill name.");
            }
        }

        string? projectName = null;
        if (request.ProjectId is int projectId)
        {
            var ownedProject = await projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (ownedProject is null)
            {
                return AdminResult<AiTeamMatchResponse>.Fail(AdminResultCode.NotFound, "Project was not found.");
            }

            if (ownedProject.ManagerUserId != managerUserId)
            {
                return AdminResult<AiTeamMatchResponse>.Fail(
                    AdminResultCode.ValidationError,
                    "You can run team matching only for projects you own.");
            }

            projectName = ownedProject.Name;
        }

        var organizationProfiles = await userProfileRepository.ListActiveWithSkillsAsync(cancellationToken);
        var activeAllocations = await allocationRepository.ListAllActiveAsync(cancellationToken);
        var allocationsByUser = activeAllocations
            .GroupBy(allocation => allocation.UserId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var matchResults = organizationTeamMatcher.MatchOrganizationTeam(
            request.Roles,
            organizationProfiles,
            allocationsByUser);

        var llmSettings = await llmConfigurationReader.ReadAsync(cancellationToken);
        var summary = await BuildTeamMatchSummaryAsync(request.Context, projectName, matchResults, llmSettings, cancellationToken);

        var roleDtos = matchResults.Select(MapTeamRoleResult).ToList();
        var response = new AiTeamMatchResponse(
            roleDtos,
            roleDtos.Count(dto => dto.IsFilled),
            roleDtos.Count,
            summary.Text,
            summary.UsedFallback,
            summary.ProviderUsed,
            projectName);

        return AdminResult<AiTeamMatchResponse>.Success(response);
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
        var summary = await projectRiskSummaryService.SummarizeAsync(facts, cancellationToken);

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
        var completion = await llmClient.CompleteAsync(prompt, llmSettings, cancellationToken);
        if (!completion.Succeeded)
        {
            return new SummaryResult(
                $"{deterministicSkillMatchSummarizer.Summarize(query, candidates)} LLM error: {completion.ErrorMessage}",
                UsedFallback: true,
                ProviderUsed: llmSettings.Provider);
        }

        if (!SkillMatchSummaryValidator.IsFaithfulToCandidates(completion.Content, candidates))
        {
            return new SummaryResult(
                deterministicSkillMatchSummarizer.Summarize(query, candidates),
                UsedFallback: true,
                ProviderUsed: llmSettings.Provider);
        }

        return new SummaryResult(completion.Content, UsedFallback: false, ProviderUsed: llmSettings.Provider);
    }

    private async Task<SummaryResult> BuildTeamMatchSummaryAsync(
        string? context,
        string? projectName,
        IReadOnlyList<TeamRoleMatchResult> roleResults,
        LlmSettings llmSettings,
        CancellationToken cancellationToken)
    {
        var combinedContext = string.IsNullOrWhiteSpace(projectName)
            ? context
            : $"Project: {projectName}. {context}".Trim();

        var llmClient = llmCompletionClientFactory.Resolve(llmSettings);
        if (llmClient is null)
        {
            return new SummaryResult(
                deterministicTeamMatchSummarizer.Summarize(roleResults),
                UsedFallback: true,
                ProviderUsed: LlmProvider.None);
        }

        var prompt = teamMatchPromptBuilder.Build(combinedContext, roleResults);
        var completion = await llmClient.CompleteAsync(prompt, llmSettings, cancellationToken);
        if (!completion.Succeeded)
        {
            return new SummaryResult(
                $"{deterministicTeamMatchSummarizer.Summarize(roleResults)} LLM error: {completion.ErrorMessage}",
                UsedFallback: true,
                ProviderUsed: llmSettings.Provider);
        }

        return new SummaryResult(completion.Content, UsedFallback: false, ProviderUsed: llmSettings.Provider);
    }

    private static TeamRoleMatchResultDto MapTeamRoleResult(TeamRoleMatchResult result)
    {
        SkillMatchCandidateDto? candidateDto = null;
        if (result.MatchedProfile is not null)
        {
            candidateDto = new SkillMatchCandidateDto(
                result.MatchedProfile.UserId,
                result.MatchedProfile.FullName,
                result.MatchedProfile.Department,
                result.MatchedProfile.Designation,
                result.MatchedProfile.ResourceStatus,
                result.MatchedProfile.CurrentUtilizationPercent,
                result.MatchScore,
                string.IsNullOrWhiteSpace(result.MatchedSkillLabel)
                    ? []
                    : [result.MatchedSkillLabel],
                string.IsNullOrWhiteSpace(result.Explanation)
                    ? result.GapReason
                    : result.Explanation);
        }

        return new TeamRoleMatchResultDto(
            result.Role.RoleTitle,
            result.Role.RequiredSkillName,
            result.Role.MinimumProficiency,
            result.IsFilled,
            result.GapType,
            result.GapReason,
            result.AvailableFromDate,
            candidateDto);
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