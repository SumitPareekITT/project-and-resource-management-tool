using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Filtering;

public sealed class SkillMatchCandidate
{
    public required UserProfile Profile { get; init; }
    public int MatchScore { get; init; }
    public bool HasQueryMatch { get; init; }
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
    public string DeterministicExplanation { get; init; } = string.Empty;
    public EmployeeStatus Status => Profile.ResourceStatus;
    public decimal CurrentUtilizationPercent => Profile.CurrentUtilizationPercent;
}