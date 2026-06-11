using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record SkillMatchCandidateDto(
    int UserId,
    string FullName,
    string Department,
    string Designation,
    EmployeeStatus Status,
    decimal CurrentUtilizationPercent,
    int MatchScore,
    IReadOnlyList<string> MatchedSkills,
    string Explanation);
