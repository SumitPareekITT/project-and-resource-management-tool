using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record AiSkillMatchResponse(
    string Query,
    IReadOnlyList<SkillMatchCandidateDto> Candidates,
    string Summary,
    bool UsedFallback,
    LlmProvider ProviderUsed);
