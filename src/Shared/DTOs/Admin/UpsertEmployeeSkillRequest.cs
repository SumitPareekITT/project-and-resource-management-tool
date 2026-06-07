using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UpsertEmployeeSkillRequest(
    int SkillId,
    ProficiencyLevel ProficiencyLevel,
    decimal? YearsOfExperience,
    DateOnly? LastUsedOn);
