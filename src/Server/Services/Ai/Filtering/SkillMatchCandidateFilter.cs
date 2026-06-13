using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Filtering;

public sealed class SkillMatchCandidateFilter
{
    private static readonly HashSet<string> GenericQueryTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "developer", "engineer", "tester", "manager", "employee", "experience",
        "intermediate", "advanced", "beginner", "expert", "need", "week", "hours"
    };

    public IReadOnlyList<SkillMatchCandidate> FilterDirectTeam(
        IReadOnlyList<UserProfile> directTeamMembers,
        string naturalLanguageQuery)
    {
        var queryTokens = SkillMatchQueryTokenizer.Tokenize(naturalLanguageQuery);

        return directTeamMembers
            .Where(profile => profile.IsActive)
            .Where(profile => profile.CurrentUtilizationPercent < BusinessRules.FullAllocationPercent)
            .Select(profile => ScoreProfile(profile, queryTokens))
            .Where(candidate => candidate.HasQueryMatch)
            .OrderByDescending(candidate => candidate.MatchScore)
            .ThenBy(candidate => candidate.CurrentUtilizationPercent)
            .ThenBy(candidate => candidate.Profile.FullName)
            .Take(10)
            .ToList();
    }

    private static SkillMatchCandidate ScoreProfile(UserProfile profile, IReadOnlyList<string> queryTokens)
    {
        var matchedSkills = new List<string>();
        var score = 0;
        var hasRoleContextMatch = false;

        foreach (var userSkill in profile.User.Skills)
        {
            var skillName = userSkill.Skill.Name;
            var categoryName = userSkill.Skill.Category.ToString();

            if (queryTokens.Count == 0)
            {
                matchedSkills.Add($"{skillName} ({userSkill.ProficiencyLevel})");
                score += MapProficiencyScore(userSkill.ProficiencyLevel);
                continue;
            }

            if (QueryMatchesSkill(queryTokens, skillName, categoryName))
            {
                matchedSkills.Add($"{skillName} ({userSkill.ProficiencyLevel})");
                score += MapProficiencyScore(userSkill.ProficiencyLevel) + 2;
            }
        }

        if (queryTokens.Count > 0)
        {
            if (queryTokens.Any(token => IsSpecificToken(token) && profile.Department.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1;
                hasRoleContextMatch = true;
            }

            if (queryTokens.Any(token => IsSpecificToken(token) && profile.Designation.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1;
                hasRoleContextMatch = true;
            }
        }

        score += profile.ResourceStatus switch
        {
            EmployeeStatus.Bench => 3,
            EmployeeStatus.PartiallyAllocated => 2,
            _ => 0
        };

        var hasQueryMatch = queryTokens.Count == 0
            || matchedSkills.Count > 0
            || hasRoleContextMatch;

        return new SkillMatchCandidate
        {
            Profile = profile,
            MatchScore = score,
            HasQueryMatch = hasQueryMatch,
            MatchedSkills = matchedSkills,
            DeterministicExplanation = BuildDeterministicExplanation(profile, matchedSkills, score)
        };
    }

    private static bool QueryMatchesSkill(IReadOnlyList<string> queryTokens, string skillName, string categoryName)
    {
        return queryTokens.Any(token =>
            skillName.Contains(token, StringComparison.OrdinalIgnoreCase)
            || categoryName.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static int MapProficiencyScore(ProficiencyLevel proficiencyLevel)
    {
        return proficiencyLevel switch
        {
            ProficiencyLevel.Expert => 4,
            ProficiencyLevel.Advanced => 3,
            ProficiencyLevel.Intermediate => 2,
            _ => 1
        };
    }

    private static string BuildDeterministicExplanation(
        UserProfile profile,
        IReadOnlyList<string> matchedSkills,
        int score)
    {
        var skillsText = matchedSkills.Count == 0
            ? "no direct skill keyword match"
            : string.Join(", ", matchedSkills);

        return $"{profile.FullName} scored {score} based on skills ({skillsText}), " +
               $"status {profile.ResourceStatus}, and utilization {profile.CurrentUtilizationPercent:0.##}%.";
    }

    private static bool IsSpecificToken(string token) => !GenericQueryTokens.Contains(token);
}
