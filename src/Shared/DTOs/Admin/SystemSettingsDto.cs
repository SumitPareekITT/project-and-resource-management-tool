namespace ProjectResourceManagement.Shared.DTOs.Admin;

/// <summary>
/// Read-only view of key system settings for the Admin configuration screen.
/// </summary>
public sealed record SystemSettingsDto(
    string LlmProvider,
    string MaskedLlmApiKey,
    int SchedulerIntervalMinutes,
    int MaxWeeklyHours);
