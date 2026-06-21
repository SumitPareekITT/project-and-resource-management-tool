namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record AiTeamMatchRequest(
    IReadOnlyList<TeamRoleRequirementDto> Roles,
    int? ProjectId = null,
    string? Context = null);
