using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Filtering;

namespace ProjectResourceManagement.Server.Services.Ai.Prompts;

public sealed class SkillMatchPromptBuilder
{
    public LlmCompletionRequest Build(string query, IReadOnlyList<SkillMatchCandidate> candidates)
    {
        var systemInstruction =
            "You are a resource planning assistant. You must ONLY discuss the exact candidates listed below. " +
            "Never invent employees, user IDs, skills, or utilization values. " +
            "Do not create markdown tables. Write 2-4 short sentences explaining why the listed people match the query.";

        var candidateFacts = candidates.Count == 0
            ? ["No candidates matched the manager query."]
            : candidates
                .Select((candidate, index) =>
                    $"{index + 1}. UserId={candidate.Profile.UserId}; Name={candidate.Profile.FullName}; " +
                    $"Department={candidate.Profile.Department}; Designation={candidate.Profile.Designation}; " +
                    $"Status={candidate.Profile.ResourceStatus}; Utilization={candidate.CurrentUtilizationPercent:0.##}%; " +
                    $"MatchScore={candidate.MatchScore}; Skills=[{string.Join(", ", candidate.MatchedSkills)}]")
                .ToList();

        var userPrompt =
            $"Manager query: {query}\n\n" +
            "Verified direct-team candidates (use ONLY these people):\n" +
            string.Join("\n", candidateFacts) +
            "\n\nIf the list is empty, say no matches were found. Otherwise explain the listed candidates briefly in plain text.";

        return new LlmCompletionRequest(systemInstruction, userPrompt);
    }
}
