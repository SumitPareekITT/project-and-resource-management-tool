using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record ResourceDashboardRowDto(
    int EmployeeId,
    string FullName,
    string Department,
    string Designation,
    decimal CurrentUtilizationPercent,
    ResourceDashboardCategory Category,
    string ActiveAllocationsSummary);
