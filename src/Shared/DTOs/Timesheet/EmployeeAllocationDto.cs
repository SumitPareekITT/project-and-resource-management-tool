using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record EmployeeAllocationDto(
    int AllocationId,
    int ProjectId,
    string ProjectName,
    decimal UtilizationPercentage,
    DateOnly FromDate,
    DateOnly? ToDate,
    AllocationStatus Status);
