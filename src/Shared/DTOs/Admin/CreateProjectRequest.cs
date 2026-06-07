namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record CreateProjectRequest(
    string Name,
    string ClientName,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int ManagerId,
    int TotalStoryPoints);
