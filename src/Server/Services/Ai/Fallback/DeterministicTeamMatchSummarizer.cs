using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Fallback;

public sealed class DeterministicTeamMatchSummarizer
{
    public string Summarize(IReadOnlyList<TeamRoleMatchResult> roleResults)
    {
        var filled = roleResults.Count(result => result.IsFilled);
        var total = roleResults.Count;
        var lines = new List<string>
        {
            $"Organization team match completed: {filled}/{total} roles filled in a single pass (no duplicate assignments)."
        };

        foreach (var result in roleResults)
        {
            if (result.IsFilled && result.MatchedProfile is not null)
            {
                lines.Add(
                    $"✓ {result.Role.RoleTitle}: {result.MatchedProfile.FullName} ({result.MatchedSkillLabel}, {result.MatchedProfile.CurrentUtilizationPercent:0.##}% utilized)");
                continue;
            }

            var gapLabel = result.GapType switch
            {
                TeamRoleGapType.SkillGap => "Skill gap",
                TeamRoleGapType.AvailabilityGap => "Availability gap",
                _ => "Gap"
            };

            lines.Add($"✗ {result.Role.RoleTitle}: {gapLabel} — {result.GapReason}");
        }

        return string.Join("\n", lines);
    }
}
