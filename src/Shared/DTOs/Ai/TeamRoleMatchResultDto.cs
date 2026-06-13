using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record TeamRoleMatchResultDto(
    string RoleTitle,
    string RequiredSkillName,
    ProficiencyLevel MinimumProficiency,
    bool IsFilled,
    TeamRoleGapType GapType,
    string GapReason,
    DateOnly? AvailableFromDate,
    SkillMatchCandidateDto? SuggestedCandidate);
