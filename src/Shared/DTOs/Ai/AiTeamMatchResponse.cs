using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record AiTeamMatchResponse(
    IReadOnlyList<TeamRoleMatchResultDto> RoleResults,
    int FilledCount,
    int TotalRoles,
    string Summary,
    bool UsedFallback,
    LlmProvider ProviderUsed,
    string? ProjectName);
