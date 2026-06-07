using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record MilestoneDto(
    int MilestoneId,
    int ProjectId,
    string Title,
    string Description,
    DateOnly DueDate,
    MilestoneStatus Status,
    int StoryPoints,
    int CompletedStoryPoints);
