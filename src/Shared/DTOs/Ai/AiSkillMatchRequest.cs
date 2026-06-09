namespace ProjectResourceManagement.Shared.DTOs.Ai;

public sealed record AiSkillMatchRequest(
    string Query,
    int? ProjectId = null);
