namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record CreateAllocationRequest(
    int ProjectId,
    int UserId,
    decimal UtilizationPercentage,
    DateOnly FromDate,
    DateOnly? ToDate);
