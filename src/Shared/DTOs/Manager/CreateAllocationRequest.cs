namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record CreateAllocationRequest(
    int ProjectId,
    int EmployeeId,
    decimal UtilizationPercentage,
    DateOnly FromDate,
    DateOnly? ToDate);
