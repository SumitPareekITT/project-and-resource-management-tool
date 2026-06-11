using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record ActivityTagOptionDto(int TagId, string Name, SkillCategory Category);
