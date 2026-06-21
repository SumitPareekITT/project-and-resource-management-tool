using System.Text.RegularExpressions;
using ProjectResourceManagement.Server.Services.Ai.Filtering;

namespace ProjectResourceManagement.Server.Services.Ai.Fallback;

public static partial class SkillMatchSummaryValidator
{
    public static bool IsFaithfulToCandidates(string summary, IReadOnlyList<SkillMatchCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return false;
        }

        foreach (Match match in UserIdPattern().Matches(summary))
        {
            if (!int.TryParse(match.Groups[1].Value, out var userId))
            {
                continue;
            }

            if (!candidates.Any(candidate => candidate.Profile.UserId == userId))
            {
                return false;
            }
        }

        var tableRowCount = CountMarkdownTableDataRows(summary);
        if (tableRowCount > candidates.Count)
        {
            return false;
        }

        if (candidates.Count == 0)
        {
            return !tableRowCount.HasValue || tableRowCount.Value == 0;
        }

        var allowedNames = candidates
            .Select(candidate => candidate.Profile.FullName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in NamePattern().Matches(summary))
        {
            var mentionedName = match.Groups[1].Value.Trim();
            if (mentionedName.Length < 3)
            {
                continue;
            }

            if (!allowedNames.Contains(mentionedName))
            {
                return false;
            }
        }

        return true;
    }

    private static int? CountMarkdownTableDataRows(string summary)
    {
        var rows = summary
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith('|') && line.EndsWith('|'))
            .Where(line => !line.Contains("---"))
            .Where(line => !line.Contains("Rank", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return rows.Count == 0 ? null : rows.Count;
    }

    [GeneratedRegex(@"\bUserId\s*[=|:]\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex UserIdPattern();

    [GeneratedRegex(@"\*\*([^*]+)\*\*")]
    private static partial Regex NamePattern();
}
