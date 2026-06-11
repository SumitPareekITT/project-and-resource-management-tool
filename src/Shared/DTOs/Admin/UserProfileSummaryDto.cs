using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UserProfileSummaryDto(
    int ProfileId,
    int UserId,
    string FullName,
    string Email,
    string Department,
    string Designation,
    EmployeeStatus ResourceStatus,
    decimal CurrentUtilizationPercent,
    bool IsActive,
    int? ManagerUserId,
    string? ManagerName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<UserSkillDto> Skills);
