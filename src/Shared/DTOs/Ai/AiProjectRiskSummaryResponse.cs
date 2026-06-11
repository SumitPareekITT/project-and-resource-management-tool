using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record AiProjectRiskSummaryResponse(
    int ProjectId,
    string ProjectName,
    ProjectHealthStatus HealthStatus,
    IReadOnlyList<string> FactLines,
    string Summary,
    bool UsedFallback,
    LlmProvider ProviderUsed);
