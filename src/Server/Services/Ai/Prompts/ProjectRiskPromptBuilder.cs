using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Facts;

namespace ProjectResourceManagement.Server.Services.Ai.Prompts;

public sealed class ProjectRiskPromptBuilder
{
    public LlmCompletionRequest Build(ProjectRiskFacts facts)
    {
        var systemInstruction =
            "You are a project risk analyst. Summarize delivery risk using only the factual lines provided. " +
            "Do not assume missing data. Keep the response concise and actionable.";

        var userPrompt =
            $"Project: {facts.ProjectName} ({facts.ClientName})\n" +
            $"Status: {facts.Status}; Health: {facts.HealthStatus}\n" +
            $"Timeline: {facts.StartDate:yyyy-MM-dd} to {facts.EndDate:yyyy-MM-dd}\n" +
            $"Story points: {facts.CompletedStoryPoints}/{facts.TotalStoryPoints}\n" +
            $"Active allocations: {facts.ActiveAllocationCount}\n" +
            $"Previous week hours: {facts.PreviousWeekLoggedHours:0.##}/{facts.PreviousWeekExpectedHours:0.##}\n\n" +
            "Milestones:\n" +
            string.Join("\n", facts.MilestoneLines.Select(line => $"- {line}")) + "\n\n" +
            "Allocations:\n" +
            string.Join("\n", facts.AllocationLines.Select(line => $"- {line}")) + "\n\n" +
            "Provide a short risk summary with key concerns and recommended manager actions.";

        return new LlmCompletionRequest(systemInstruction, userPrompt);
    }
}
