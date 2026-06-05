using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Data;

public static class SeedData
{
    private static readonly DateTime SeededAtUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<User> Users { get; } =
    [
        new()
        {
            Id = 1,
            FullName = "System Admin",
            Email = "admin@techserve.local",
            Username = "admin",
            PasswordHash = "CHANGE_ME_WITH_PASSWORD_HASHER",
            Role = UserRole.Admin,
            ForcePasswordChange = true,
            IsActive = true,
            CreatedAtUtc = SeededAtUtc
        }
    ];

    public static IReadOnlyList<ActivityTag> ActivityTags { get; } =
    [
        new() { Id = 1, Name = "Backend API Development", Category = SkillCategory.Backend },
        new() { Id = 2, Name = "Microservices / Architecture", Category = SkillCategory.Backend },
        new() { Id = 3, Name = "Database Design & Queries", Category = SkillCategory.Backend },
        new() { Id = 4, Name = "WebSocket / Real-time Features", Category = SkillCategory.Backend },
        new() { Id = 5, Name = "Frontend Development", Category = SkillCategory.Frontend },
        new() { Id = 6, Name = "Code Review / Mentoring", Category = SkillCategory.Other },
        new() { Id = 7, Name = "Bug Fixing", Category = SkillCategory.Other },
        new() { Id = 8, Name = "DevOps / Deployment", Category = SkillCategory.DevOps },
        new() { Id = 9, Name = "Testing & QA", Category = SkillCategory.QA },
        new() { Id = 10, Name = "Documentation", Category = SkillCategory.Other }
    ];

    public static IReadOnlyList<SystemConfiguration> SystemConfigurations { get; } =
    [
        new()
        {
            Id = 1,
            Key = "MaxWeeklyHours",
            Value = "40",
            Description = "Maximum weekly hours available for allocation and timesheet validation.",
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 2,
            Key = "SchedulerIntervalMinutes",
            Value = "60",
            Description = "Interval for utilization and project health background jobs.",
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 3,
            Key = "LlmProvider",
            Value = "None",
            Description = "Configured AI provider. Expected values later: Gemini or Groq.",
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 4,
            Key = "LlmApiKey",
            Value = string.Empty,
            Description = "Encrypted or externally supplied LLM API key.",
            UpdatedAtUtc = SeededAtUtc
        }
    ];
}
