using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Admin;

/// <summary>
/// Handles Admin updates to runtime system settings stored in SystemConfigurations table.
/// </summary>
public sealed class SystemConfigurationAdminService(SystemConfigurationRepository configurationRepository)
{
    private static readonly HashSet<string> AllowedKeys =
    [
        "LlmProvider",
        "LlmApiKey",
        "SchedulerIntervalMinutes",
        "MaxWeeklyHours"
    ];

    public async Task<AdminResult<SystemSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var provider = await ReadValueAsync("LlmProvider", cancellationToken) ?? "None";
        var apiKey = await ReadValueAsync("LlmApiKey", cancellationToken) ?? string.Empty;
        var schedulerMinutes = await ReadIntAsync("SchedulerIntervalMinutes", BusinessRules.DefaultSchedulerIntervalMinutes, cancellationToken);
        var maxWeeklyHours = await ReadIntAsync("MaxWeeklyHours", BusinessRules.DefaultMaxWeeklyHours, cancellationToken);

        return AdminResult<SystemSettingsDto>.Success(new SystemSettingsDto(
            provider,
            MaskApiKey(apiKey),
            schedulerMinutes,
            maxWeeklyHours));
    }

    public async Task<AdminResult<SystemSettingsDto>> UpdateSettingAsync(
        string key,
        UpdateSystemSettingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedKeys.Contains(key))
        {
            return AdminResult<SystemSettingsDto>.Fail(AdminResultCode.ValidationError, "Unknown configuration key.");
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return AdminResult<SystemSettingsDto>.Fail(AdminResultCode.ValidationError, "Value is required.");
        }

        var validationError = ValidateSettingValue(key, request.Value.Trim());
        if (validationError is not null)
        {
            return validationError;
        }

        var configuration = await configurationRepository.GetByKeyAsync(key, cancellationToken);
        if (configuration is null)
        {
            return AdminResult<SystemSettingsDto>.Fail(AdminResultCode.NotFound, "Configuration key was not found.");
        }

        configuration.Value = request.Value.Trim();
        configuration.UpdatedAtUtc = DateTime.UtcNow;
        await configurationRepository.SaveChangesAsync(cancellationToken);

        return await GetSettingsAsync(cancellationToken);
    }

    private static AdminResult<SystemSettingsDto>? ValidateSettingValue(string key, string value)
    {
        return key switch
        {
            "LlmProvider" when !Enum.TryParse<LlmProvider>(value, ignoreCase: true, out _)
                => AdminResult<SystemSettingsDto>.Fail(AdminResultCode.ValidationError, "LlmProvider must be None, Gemini, or Groq."),
            "SchedulerIntervalMinutes" when !int.TryParse(value, out var minutes) || minutes <= 0
                => AdminResult<SystemSettingsDto>.Fail(AdminResultCode.ValidationError, "Scheduler interval must be a positive number of minutes."),
            "MaxWeeklyHours" when !int.TryParse(value, out var hours) || hours <= 0
                => AdminResult<SystemSettingsDto>.Fail(AdminResultCode.ValidationError, "Max weekly hours must be a positive number."),
            _ => null
        };
    }

    private async Task<string?> ReadValueAsync(string key, CancellationToken cancellationToken)
    {
        var configuration = await configurationRepository.GetByKeyAsync(key, cancellationToken);
        return configuration?.Value;
    }

    private async Task<int> ReadIntAsync(string key, int defaultValue, CancellationToken cancellationToken)
    {
        var rawValue = await ReadValueAsync(key, cancellationToken);
        return int.TryParse(rawValue, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "(not set)";
        }

        return new string('*', Math.Min(apiKey.Length, 20));
    }
}
