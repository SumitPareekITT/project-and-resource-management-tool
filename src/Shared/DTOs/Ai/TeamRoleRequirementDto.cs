using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record TeamRoleRequirementDto(
    string RoleTitle,
    string RequiredSkillName,
    ProficiencyLevel MinimumProficiency);
