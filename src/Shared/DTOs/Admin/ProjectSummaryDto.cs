using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record ProjectSummaryDto(
    int ProjectId,
    string Name,
    string ClientName,
    ProjectStatus Status,
    ProjectHealthStatus HealthStatus,
    int ManagerUserId,
    string ManagerName,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalStoryPoints,
    int CompletedStoryPoints,
    string StoryPointProgress,
    IReadOnlyList<MilestoneDto> Milestones);
