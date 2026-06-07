using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UpdateProjectRequest(
    string Name,
    string ClientName,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    int ManagerId,
    int TotalStoryPoints,
    int CompletedStoryPoints);
