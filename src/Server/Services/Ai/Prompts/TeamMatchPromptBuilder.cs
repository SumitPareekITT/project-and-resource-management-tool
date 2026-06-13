using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Filtering;

namespace ProjectResourceManagement.Server.Services.Ai.Prompts;

public sealed class TeamMatchPromptBuilder
{
    public LlmCompletionRequest Build(
        string? context,
        IReadOnlyList<TeamRoleMatchResult> roleResults)
    {
        var roleFacts = roleResults.Select(result =>
        {
            if (result.IsFilled && result.MatchedProfile is not null)
            {
                return
                    $"Role={result.Role.RoleTitle}; Skill={result.Role.RequiredSkillName}; Status=Filled; " +
                    $"Employee={result.MatchedProfile.FullName}; UserId={result.MatchedProfile.UserId}; " +
                    $"Utilization={result.MatchedProfile.CurrentUtilizationPercent:0.##}%; Match={result.MatchedSkillLabel}";
            }

            return
                $"Role={result.Role.RoleTitle}; Skill={result.Role.RequiredSkillName}; Status=Gap; " +
                $"GapType={result.GapType}; Reason={result.GapReason}";
        });

        var systemInstruction =
            "You are a resource planning assistant for multi-role team staffing. " +
            "Use only the role results provided. Do not invent employees. " +
            "Summarize filled roles, gaps, and recommended next steps for the manager.";

        var userPrompt =
            $"Team staffing context: {context ?? "Organization-wide team search"}\n\n" +
            "Single-pass match results (each employee used at most once):\n" +
            string.Join("\n", roleFacts) +
            "\n\nReturn concise markdown: filled roles, unfilled roles with gap type (Skill vs Availability), and manager action items.";

        return new LlmCompletionRequest(systemInstruction, userPrompt);
    }
}
