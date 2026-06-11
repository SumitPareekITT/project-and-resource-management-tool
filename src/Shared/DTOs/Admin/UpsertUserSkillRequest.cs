using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UpsertUserSkillRequest(
    int SkillId,
    ProficiencyLevel ProficiencyLevel,
    decimal? YearsOfExperience,
    DateOnly? LastUsedOn);
