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

        var topNames = string.Join(", ", candidates.Take(3).Select(candidate => candidate.Profile.FullName));
        return $"LLM provider is not configured. Returning deterministic pre-filter ranking for '{query}'. Top matches: {topNames}.";
    }
}
