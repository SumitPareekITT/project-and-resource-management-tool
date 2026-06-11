using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Ai;
using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Server.Services.Ai.Fallback;
using ProjectResourceManagement.Server.Services.Ai.Prompts;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Ai;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class AiAssistantServiceTests
{
    [Fact]
    public async Task MatchSkillsAsync_UsesFallback_WhenLlmIsNotConfigured()
    {
        await using var dbContext = CreateDbContext();
        await SeedAiScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.MatchSkillsAsync(2, new AiSkillMatchRequest("backend api"));

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.UsedFallback);
        Assert.Equal(LlmProvider.None, result.Value.ProviderUsed);
        Assert.NotEmpty(result.Value.Candidates);
        Assert.Contains("deterministic", result.Value.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchSkillsAsync_Fails_WhenQueryIsEmpty()
    {
        await using var dbContext = CreateDbContext();
        await SeedAiScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.MatchSkillsAsync(2, new AiSkillMatchRequest("  "));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminResultCode.ValidationError, result.Code);
    }

    [Fact]
    public async Task SummarizeProjectRiskAsync_UsesFallback_WithFactualLines()
    {
        await using var dbContext = CreateDbContext();
        await SeedAiScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.SummarizeProjectRiskAsync(2, new AiProjectRiskSummaryRequest(1));

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.UsedFallback);
        Assert.Equal("Apollo", result.Value.ProjectName);
        Assert.Contains(result.Value.FactLines, line => line.Contains("StoryPoints="));
        Assert.Contains("LLM provider is not configured", result.Value.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SummarizeProjectRiskAsync_Fails_WhenProjectNotOwned()
    {
        await using var dbContext = CreateDbContext();
        await SeedAiScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.SummarizeProjectRiskAsync(99, new AiProjectRiskSummaryRequest(1));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminResultCode.NotFound, result.Code);
    }

    private static async Task SeedAiScenarioAsync(ApplicationDbContext dbContext)
    {
        SchemaV3TestHelpers.SeedUser(dbContext, 2, "manager", "Manager", "m@test", UserRole.Manager);
        SchemaV3TestHelpers.SeedUser(dbContext, 20, "emp1", "Employee User", "e1@test", UserRole.Employee);
        await dbContext.SaveChangesAsync();
        dbContext.UserProfiles.Single(p => p.UserId == 20).ManagerUserId = 2;

        var backendSkill = new Skill
        {
            Id = 1,
            Name = "Backend API Development",
            Category = SkillCategory.Backend
        };
        dbContext.Skills.Add(backendSkill);

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerUserId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            Status = ProjectStatus.Active,
            HealthStatus = ProjectHealthStatus.AtRisk,
            TotalStoryPoints = 100,
            CompletedStoryPoints = 30
        });
        var employee = dbContext.UserProfiles.Single(p => p.UserId == 20);
        employee.ManagerUserId = 2;
        employee.CurrentUtilizationPercent = 50;
        employee.ResourceStatus = EmployeeStatus.PartiallyAllocated;
dbContext.UserSkills.Add(new UserSkill
        {
            UserId = 20,
            SkillId = 1,
            ProficiencyLevel = ProficiencyLevel.Advanced,
            Skill = backendSkill
        });

        dbContext.Allocations.Add(new Allocation
        {
            Id = 1,
            UserId = 20,
            ProjectId = 1,
            CreatedByUserId = 2,
            UtilizationPercentage = 50,
            FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            Status = AllocationStatus.Active
        });

        await dbContext.SaveChangesAsync();
    }

    private static AiAssistantService CreateService(ApplicationDbContext dbContext)
    {
        return new AiAssistantService(
            new UserProfileRepository(dbContext),
            new ProjectRepository(dbContext),
            new SkillMatchCandidateFilter(),
            new ProjectRiskFactAssembler(
                new ProjectRepository(dbContext),
                new AllocationRepository(dbContext),
                new TimesheetRepository(dbContext),
                new SystemConfigurationRepository(dbContext)),
            new SkillMatchPromptBuilder(),
            new ProjectRiskPromptBuilder(),
            new DeterministicSkillMatchSummarizer(),
            new DeterministicProjectRiskSummarizer(),
            new LlmConfigurationReader(new SystemConfigurationRepository(dbContext)),
            new LlmCompletionClientFactory(Array.Empty<ILlmCompletionClient>()));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
