using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data;

public static class SeedData
{
    private static readonly DateTime SeededAtUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<User> Users { get; } =
    [
        new()
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "CHANGE_ME_WITH_PASSWORD_HASHER",
            ForcePasswordChange = true,
            IsActive = true,
            CreatedAtUtc = SeededAtUtc
        }
    ];

    public static IReadOnlyList<ActivityTag> ActivityTags { get; } =
    [
        new() { Id = 1, Name = "Backend API Development", Category = Shared.Enums.SkillCategory.Backend },
        new() { Id = 2, Name = "Microservices / Architecture", Category = Shared.Enums.SkillCategory.Backend },
        new() { Id = 3, Name = "Database Design & Queries", Category = Shared.Enums.SkillCategory.Backend },
        new() { Id = 4, Name = "WebSocket / Real-time Features", Category = Shared.Enums.SkillCategory.Backend },
        new() { Id = 5, Name = "Frontend Development", Category = Shared.Enums.SkillCategory.Frontend },
        new() { Id = 6, Name = "Code Review / Mentoring", Category = Shared.Enums.SkillCategory.Other },
        new() { Id = 7, Name = "Bug Fixing", Category = Shared.Enums.SkillCategory.Other },
        new() { Id = 8, Name = "DevOps / Deployment", Category = Shared.Enums.SkillCategory.DevOps },
        new() { Id = 9, Name = "Testing & QA", Category = Shared.Enums.SkillCategory.QA },
        new() { Id = 10, Name = "Documentation", Category = Shared.Enums.SkillCategory.Other }
    ];

    public static IReadOnlyList<Skill> Skills { get; } =
    [
        new() { Id = 1, Name = "Java", Category = Shared.Enums.SkillCategory.Backend, IsActive = true },
        new() { Id = 2, Name = "Spring Boot", Category = Shared.Enums.SkillCategory.Backend, IsActive = true },
        new() { Id = 3, Name = "MySQL", Category = Shared.Enums.SkillCategory.Backend, IsActive = true },
        new() { Id = 4, Name = "WebSocket", Category = Shared.Enums.SkillCategory.Backend, IsActive = true },
        new() { Id = 5, Name = "Frontend Development", Category = Shared.Enums.SkillCategory.Frontend, IsActive = true },
        new() { Id = 6, Name = "React", Category = Shared.Enums.SkillCategory.Frontend, IsActive = true },
        new() { Id = 7, Name = "DevOps / Deployment", Category = Shared.Enums.SkillCategory.DevOps, IsActive = true },
        new() { Id = 8, Name = "Kubernetes", Category = Shared.Enums.SkillCategory.DevOps, IsActive = true },
        new() { Id = 9, Name = "Testing & QA", Category = Shared.Enums.SkillCategory.QA, IsActive = true },
        new() { Id = 10, Name = "Documentation", Category = Shared.Enums.SkillCategory.Other, IsActive = true }
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
            Description = "Configured AI provider. Values: None, Gemini, Groq, Gemma.",
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 4,
            Key = "LlmApiKey",
            Value = string.Empty,
            Description = "Encrypted or externally supplied LLM API key.",
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 5,
            Key = "LlmGemmaEndpoint",
            Value = "http://164.52.211.238/api/generate",
            Description = "Gemma server URL (POST /api/generate). API key is sent in the apikey header.",
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 6,
            Key = "LlmGemmaModel",
            Value = "gemma3:12b-it-q8_0",
            Description = "Gemma model name as registered in Ollama.",
            UpdatedAtUtc = SeededAtUtc
        }
    ];
}
