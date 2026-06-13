using ProjectResourceManagement.Server.Services.Ai.Filtering;

namespace ProjectResourceManagement.Server.Services.Ai.Fallback;

public sealed class DeterministicSkillMatchSummarizer
{
    public string Summarize(string query, IReadOnlyList<SkillMatchCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return $"No available direct-team candidates matched '{query}'. Try broader skill keywords or check bench resources.";
        }

        var lines = candidates
            .Select((candidate, index) =>
                $"{index + 1}. {candidate.Profile.FullName} (UserId {candidate.Profile.UserId}) — {candidate.DeterministicExplanation}");

        return "Verified matches from your direct team:\n" + string.Join("\n", lines);
    }
}
