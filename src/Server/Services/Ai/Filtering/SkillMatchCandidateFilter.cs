using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Ai.Filtering;

public sealed class SkillMatchCandidateFilter
{
    public IReadOnlyList<SkillMatchCandidate> FilterDirectTeam(
        IReadOnlyList<Employee> directTeamMembers,
        string naturalLanguageQuery)
    {
        var queryTokens = SkillMatchQueryTokenizer.Tokenize(naturalLanguageQuery);

        return directTeamMembers
            .Where(employee => employee.IsActive)
            .Where(employee => employee.CurrentUtilizationPercent < BusinessRules.FullAllocationPercent)
            .Select(employee => ScoreEmployee(employee, queryTokens))
            .Where(candidate => candidate.MatchScore > 0 || queryTokens.Count == 0)
            .OrderByDescending(candidate => candidate.MatchScore)
            .ThenBy(candidate => candidate.CurrentUtilizationPercent)
            .ThenBy(candidate => candidate.Employee.FullName)
            .Take(10)
            .ToList();
    }

    private static SkillMatchCandidate ScoreEmployee(Employee employee, IReadOnlyList<string> queryTokens)
    {
        var matchedSkills = new List<string>();
        var score = 0;

        foreach (var employeeSkill in employee.Skills)
        {
            var skillName = employeeSkill.Skill.Name;
            var categoryName = employeeSkill.Skill.Category.ToString();

            if (queryTokens.Count == 0)
            {
                matchedSkills.Add($"{skillName} ({employeeSkill.ProficiencyLevel})");
                score += MapProficiencyScore(employeeSkill.ProficiencyLevel);
                continue;
            }

            if (QueryMatchesSkill(queryTokens, skillName, categoryName))
            {
                matchedSkills.Add($"{skillName} ({employeeSkill.ProficiencyLevel})");
                score += MapProficiencyScore(employeeSkill.ProficiencyLevel) + 2;
            }
        }

        if (queryTokens.Count > 0)
        {
            if (queryTokens.Any(token => employee.Department.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1;
            }

            if (queryTokens.Any(token => employee.Designation.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1;
            }
        }

        score += employee.Status switch
        {
            EmployeeStatus.Bench => 3,
            EmployeeStatus.PartiallyAllocated => 2,
            _ => 0
        };

        return new SkillMatchCandidate
        {
            Employee = employee,
            MatchScore = score,
            MatchedSkills = matchedSkills,
            DeterministicExplanation = BuildDeterministicExplanation(employee, matchedSkills, score)
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
        Employee employee,
        IReadOnlyList<string> matchedSkills,
        int score)
    {
        var skillsText = matchedSkills.Count == 0
            ? "no direct skill keyword match"
            : string.Join(", ", matchedSkills);

        return $"{employee.FullName} scored {score} based on skills ({skillsText}), " +
               $"status {employee.Status}, and utilization {employee.CurrentUtilizationPercent:0.##}%.";
    }
}
