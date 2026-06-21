using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Filtering;

/// <summary>
/// Single-pass organization-wide team matcher. Each employee is assigned to at most one role per search.
/// </summary>
public sealed class OrganizationTeamMatcher
{
    public IReadOnlyList<TeamRoleMatchResult> MatchOrganizationTeam(
        IReadOnlyList<TeamRoleRequirementDto> roles,
        IReadOnlyList<UserProfile> organizationProfiles,
        IReadOnlyDictionary<int, List<Allocation>> activeAllocationsByUser)
    {
        var assignedUserIds = new HashSet<int>();
        var results = new List<TeamRoleMatchResult>();

        foreach (var role in roles)
        {
            var result = MatchRole(role, organizationProfiles, activeAllocationsByUser, assignedUserIds);
            results.Add(result);

            if (result.IsFilled && result.MatchedProfile is not null)
            {
                assignedUserIds.Add(result.MatchedProfile.UserId);
            }
        }

        return results;
    }

    private static TeamRoleMatchResult MatchRole(
        TeamRoleRequirementDto role,
        IReadOnlyList<UserProfile> organizationProfiles,
        IReadOnlyDictionary<int, List<Allocation>> activeAllocationsByUser,
        ISet<int> assignedUserIds)
    {
        var skillHolders = organizationProfiles
            .Where(profile => profile.IsActive)
            .Select(profile => ScoreForRole(profile, role))
            .Where(entry => entry.HasRequiredSkill)
            .ToList();

        if (skillHolders.Count == 0)
        {
            return TeamRoleMatchResult.Unfilled(
                role,
                TeamRoleGapType.SkillGap,
                $"No employee in the organization has {role.RequiredSkillName} at {role.MinimumProficiency} level or above. Consider hiring or training.",
                null,
                null);
        }

        var available = skillHolders
            .Where(entry => entry.Profile.CurrentUtilizationPercent < BusinessRules.FullAllocationPercent)
            .Where(entry => !assignedUserIds.Contains(entry.Profile.UserId))
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Profile.CurrentUtilizationPercent)
            .ThenBy(entry => entry.Profile.FullName)
            .ToList();

        if (available.Count > 0)
        {
            var best = available[0];
            return TeamRoleMatchResult.Filled(role, best.Profile, best.Score, best.MatchedSkillLabel, best.Explanation);
        }

        var unavailable = skillHolders
            .Where(entry => !assignedUserIds.Contains(entry.Profile.UserId))
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => GetEarliestReleaseDate(entry.Profile.UserId, activeAllocationsByUser))
            .ToList();

        if (unavailable.Count == 0)
        {
            return TeamRoleMatchResult.Unfilled(
                role,
                TeamRoleGapType.AvailabilityGap,
                $"Employees with {role.RequiredSkillName} were already selected for another role in this team search.",
                null,
                null);
        }

        var candidate = unavailable[0];
        var releaseDate = GetEarliestReleaseDate(candidate.Profile.UserId, activeAllocationsByUser);
        var projectName = GetBlockingProjectName(candidate.Profile.UserId, activeAllocationsByUser, releaseDate);
        var reason = releaseDate is null
            ? $"{candidate.Profile.FullName} has {role.RequiredSkillName} but is fully allocated with open-ended assignments."
            : $"{candidate.Profile.FullName} has {role.RequiredSkillName} but is allocated on {projectName ?? "another project"} until {releaseDate:yyyy-MM-dd}.";

        return TeamRoleMatchResult.Unfilled(
            role,
            TeamRoleGapType.AvailabilityGap,
            reason,
            releaseDate,
            candidate.Profile,
            candidate.Score,
            candidate.MatchedSkillLabel,
            candidate.Explanation);
    }

    private static RoleSkillScore ScoreForRole(UserProfile profile, TeamRoleRequirementDto role)
    {
        UserSkill? bestSkill = null;
        foreach (var userSkill in profile.User.Skills)
        {
            if (!SkillMatches(userSkill.Skill.Name, role.RequiredSkillName))
            {
                continue;
            }

            if ((int)userSkill.ProficiencyLevel < (int)role.MinimumProficiency)
            {
                continue;
            }

            if (bestSkill is null || userSkill.ProficiencyLevel > bestSkill.ProficiencyLevel)
            {
                bestSkill = userSkill;
            }
        }

        if (bestSkill is null)
        {
            return RoleSkillScore.NoMatch(profile);
        }

        var score = (int)bestSkill.ProficiencyLevel * 10;
        score += profile.ResourceStatus switch
        {
            EmployeeStatus.Bench => 8,
            EmployeeStatus.PartiallyAllocated => 4,
            _ => 0
        };
        score += (int)Math.Max(0, BusinessRules.FullAllocationPercent - profile.CurrentUtilizationPercent) / 10;
        if (bestSkill.YearsOfExperience is > 0)
        {
            score += (int)Math.Min(bestSkill.YearsOfExperience.Value, 5);
        }

        var label = $"{bestSkill.Skill.Name} ({bestSkill.ProficiencyLevel})";
        var explanation =
            $"{profile.FullName} matched {label} with utilization {profile.CurrentUtilizationPercent:0.##}% and status {profile.ResourceStatus}.";

        return new RoleSkillScore(profile, true, score, label, explanation);
    }

    private static bool SkillMatches(string skillName, string requiredSkillName)
    {
        return skillName.Equals(requiredSkillName, StringComparison.OrdinalIgnoreCase)
            || skillName.Contains(requiredSkillName, StringComparison.OrdinalIgnoreCase)
            || requiredSkillName.Contains(skillName, StringComparison.OrdinalIgnoreCase);
    }

    private static DateOnly? GetEarliestReleaseDate(
        int userId,
        IReadOnlyDictionary<int, List<Allocation>> activeAllocationsByUser)
    {
        if (!activeAllocationsByUser.TryGetValue(userId, out var allocations) || allocations.Count == 0)
        {
            return null;
        }

        var datedEnds = allocations
            .Where(allocation => allocation.ToDate is not null)
            .Select(allocation => allocation.ToDate!.Value)
            .OrderBy(date => date)
            .ToList();

        return datedEnds.Count == 0 ? null : datedEnds[0].AddDays(1);
    }

    private static string? GetBlockingProjectName(
        int userId,
        IReadOnlyDictionary<int, List<Allocation>> activeAllocationsByUser,
        DateOnly? releaseDate)
    {
        if (!activeAllocationsByUser.TryGetValue(userId, out var allocations))
        {
            return null;
        }

        var blocking = releaseDate is null
            ? allocations.FirstOrDefault()
            : allocations.FirstOrDefault(allocation => allocation.ToDate?.AddDays(1) == releaseDate);

        return blocking?.Project.Name;
    }

    private sealed record RoleSkillScore(
        UserProfile Profile,
        bool HasRequiredSkill,
        int Score,
        string MatchedSkillLabel,
        string Explanation)
    {
        public static RoleSkillScore NoMatch(UserProfile profile) =>
            new(profile, false, 0, string.Empty, string.Empty);
    }
}

public sealed class TeamRoleMatchResult
{
    public required TeamRoleRequirementDto Role { get; init; }
    public bool IsFilled { get; init; }
    public TeamRoleGapType GapType { get; init; }
    public string GapReason { get; init; } = string.Empty;
    public DateOnly? AvailableFromDate { get; init; }
    public UserProfile? MatchedProfile { get; init; }
    public int MatchScore { get; init; }
    public string MatchedSkillLabel { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;

    public static TeamRoleMatchResult Filled(
        TeamRoleRequirementDto role,
        UserProfile profile,
        int score,
        string matchedSkillLabel,
        string explanation) =>
        new()
        {
            Role = role,
            IsFilled = true,
            GapType = TeamRoleGapType.None,
            GapReason = string.Empty,
            MatchedProfile = profile,
            MatchScore = score,
            MatchedSkillLabel = matchedSkillLabel,
            Explanation = explanation
        };

    public static TeamRoleMatchResult Unfilled(
        TeamRoleRequirementDto role,
        TeamRoleGapType gapType,
        string gapReason,
        DateOnly? availableFromDate,
        UserProfile? suggestedProfile,
        int score = 0,
        string matchedSkillLabel = "",
        string explanation = "") =>
        new()
        {
            Role = role,
            IsFilled = false,
            GapType = gapType,
            GapReason = gapReason,
            AvailableFromDate = availableFromDate,
            MatchedProfile = suggestedProfile,
            MatchScore = score,
            MatchedSkillLabel = matchedSkillLabel,
            Explanation = explanation
        };
}
