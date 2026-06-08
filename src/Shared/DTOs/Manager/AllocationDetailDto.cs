using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record AllocationDetailDto(
    int AllocationId,
    int EmployeeId,
    string EmployeeName,
    int ProjectId,
    string ProjectName,
    decimal UtilizationPercentage,
    DateOnly FromDate,
    DateOnly? ToDate,
    AllocationStatus Status);
