using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Manager;

public sealed record ManagerProjectHealthDto(
    int ProjectId,
    string Name,
    string ClientName,
    ProjectStatus Status,
    ProjectHealthStatus HealthStatus,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalStoryPoints,
    int CompletedStoryPoints,
    string StoryPointProgress,
    int ActiveAllocationCount,
    decimal PreviousWeekLoggedHours,
    decimal PreviousWeekExpectedHours,
    IReadOnlyList<string> HealthSignals);
