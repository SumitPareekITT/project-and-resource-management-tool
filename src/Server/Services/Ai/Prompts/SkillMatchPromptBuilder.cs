using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Filtering;

namespace ProjectResourceManagement.Server.Services.Ai.Prompts;

public sealed class SkillMatchPromptBuilder
{
    public LlmCompletionRequest Build(string query, IReadOnlyList<SkillMatchCandidate> candidates)
    {
        var candidateFacts = candidates
            .Select(candidate =>
                $"EmployeeId={candidate.Employee.Id}; Name={candidate.Employee.FullName}; " +
                $"Department={candidate.Employee.Department}; Designation={candidate.Employee.Designation}; " +
                $"Status={candidate.Employee.Status}; Utilization={candidate.CurrentUtilizationPercent:0.##}%; " +
                $"MatchScore={candidate.MatchScore}; Skills=[{string.Join(", ", candidate.MatchedSkills)}]")
            .ToList();

        var systemInstruction =
            "You are a resource planning assistant. Use only the candidate facts provided by the system. " +
            "Do not invent employees or skills. Rank candidates for the manager query and explain each match briefly.";

        var userPrompt =
            $"Manager query: {query}\n\n" +
            "Pre-filtered direct-team candidates:\n" +
            string.Join("\n", candidateFacts) +
            "\n\nReturn concise markdown with a ranked list and one-line explanation per employee.";

        return new LlmCompletionRequest(systemInstruction, userPrompt);
    }
}
