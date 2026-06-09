using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Filtering;

public sealed class SkillMatchCandidate
{
    public required Employee Employee { get; init; }
    public int MatchScore { get; init; }
    public IReadOnlyList<string> MatchedSkills { get; init; } = [];
    public string DeterministicExplanation { get; init; } = string.Empty;
    public EmployeeStatus Status => Employee.Status;
    public decimal CurrentUtilizationPercent => Employee.CurrentUtilizationPercent;
}
