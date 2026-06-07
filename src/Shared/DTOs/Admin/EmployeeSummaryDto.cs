using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record EmployeeSummaryDto(
    int EmployeeId,
    int UserId,
    string FullName,
    string Email,
    string Department,
    string Designation,
    EmployeeStatus Status,
    decimal CurrentUtilizationPercent,
    bool IsActive,
    int? ManagerId,
    string? ManagerName,
    IReadOnlyList<EmployeeSkillDto> Skills);
