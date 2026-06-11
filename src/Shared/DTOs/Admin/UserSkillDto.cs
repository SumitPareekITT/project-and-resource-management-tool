using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UserSkillDto(
    int SkillId,
    string SkillName,
    SkillCategory Category,
    ProficiencyLevel ProficiencyLevel,
    decimal? YearsOfExperience,
    DateOnly? LastUsedOn);
