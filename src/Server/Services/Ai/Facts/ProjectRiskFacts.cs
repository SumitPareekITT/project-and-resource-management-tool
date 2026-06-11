using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Facts;

public sealed record ProjectRiskFacts(
    int ProjectId,
    string ProjectName,
    string ClientName,
    ProjectStatus Status,
    ProjectHealthStatus HealthStatus,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalStoryPoints,
    int CompletedStoryPoints,
    int ActiveAllocationCount,
    decimal PreviousWeekLoggedHours,
    decimal PreviousWeekExpectedHours,
    IReadOnlyList<string> MilestoneLines,
    IReadOnlyList<string> AllocationLines);
