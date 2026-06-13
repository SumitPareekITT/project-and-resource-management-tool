using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record AddUserSkillByNameRequest(
    string SkillName,
    SkillCategory Category,
    ProficiencyLevel ProficiencyLevel,
    decimal? YearsOfExperience = null,
    DateOnly? LastUsedOn = null);
