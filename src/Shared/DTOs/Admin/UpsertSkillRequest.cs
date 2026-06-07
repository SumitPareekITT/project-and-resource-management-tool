using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UpsertSkillRequest(string Name, SkillCategory Category);
