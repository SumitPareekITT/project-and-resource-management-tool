namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record AllocationMatrixRowDto(
    int AllocationId,
    int UserId,
    string UserName,
    int ProjectId,
    string ProjectName,
    string ManagerName,
    decimal UtilizationPercentage,
    DateOnly FromDate,
    DateOnly? ToDate,
    string Status);
