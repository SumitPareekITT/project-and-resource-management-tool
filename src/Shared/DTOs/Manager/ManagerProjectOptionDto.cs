using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record ManagerProjectOptionDto(
    int ProjectId,
    string Name,
    string ClientName,
    ProjectStatus Status,
    ProjectHealthStatus HealthStatus,
    string StoryPointProgress,
    DateOnly StartDate,
    DateOnly EndDate);
