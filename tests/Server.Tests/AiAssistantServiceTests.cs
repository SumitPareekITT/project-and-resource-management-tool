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
        dbContext.Users.AddRange(
            new User
            {
                Id = 2,
                FullName = "Manager",
                Email = "m@test",
                Username = "manager",
                PasswordHash = "h",
                Role = UserRole.Manager,
                IsActive = true
            },
            new User
            {
                Id = 20,
                FullName = "Employee User",
                Email = "e1@test",
                Username = "emp1",
                PasswordHash = "h",
                Role = UserRole.Employee,
                IsActive = true
            });

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
            ManagerId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            Status = ProjectStatus.Active,
            HealthStatus = ProjectHealthStatus.AtRisk,
            TotalStoryPoints = 100,
            CompletedStoryPoints = 30
        });

        var employee = new Employee
        {
            Id = 10,
            UserId = 20,
            ManagerId = 2,
            FullName = "Employee One",
            Email = "e1@test",
            Department = "Engineering",
            Designation = "Developer",
            IsActive = true,
            CurrentUtilizationPercent = 50,
            Status = EmployeeStatus.PartiallyAllocated
        };
        dbContext.Employees.Add(employee);
        dbContext.EmployeeSkills.Add(new EmployeeSkill
        {
            EmployeeId = 10,
            SkillId = 1,
            ProficiencyLevel = ProficiencyLevel.Advanced,
            Employee = employee,
            Skill = backendSkill
        });

        dbContext.Allocations.Add(new Allocation
        {
            Id = 1,
            EmployeeId = 10,
            ProjectId = 1,
            CreatedByManagerId = 2,
            UtilizationPercentage = 50,
            FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            Status = AllocationStatus.Active
        });

        await dbContext.SaveChangesAsync();
    }

    private static AiAssistantService CreateService(ApplicationDbContext dbContext)
    {
        return new AiAssistantService(
            new EmployeeRepository(dbContext),
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
