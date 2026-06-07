using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record SkillDto(int Id, string Name, SkillCategory Category, bool IsActive);
