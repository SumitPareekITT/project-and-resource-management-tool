using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record ManagerProjectOptionDto(
    int ProjectId,
    string Name,
    string ClientName,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly EndDate);
