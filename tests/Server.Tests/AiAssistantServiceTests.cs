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
        Assert.Contains("Verified matches from your direct team", result.Value!.Summary, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task MatchOrganizationTeamAsync_FillsAvailableRolesAcrossOrganization()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamMatchScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var request = new AiTeamMatchRequest(
        [
            new TeamRoleRequirementDto("Senior Java Developer", "Java", ProficiencyLevel.Advanced),
            new TeamRoleRequirementDto("QA Tester", "QA Testing", ProficiencyLevel.Intermediate)
        ]);

        var result = await service.MatchOrganizationTeamAsync(2, request);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.FilledCount);
        Assert.Equal(2, result.Value.TotalRoles);
        Assert.True(result.Value.UsedFallback);
        Assert.All(result.Value.RoleResults.Where(role => role.IsFilled), role =>
            Assert.NotNull(role.SuggestedCandidate));
    }

    [Fact]
    public async Task MatchOrganizationTeamAsync_ReturnsPartialResults_WithSkillGap()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamMatchScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var request = new AiTeamMatchRequest(
        [
            new TeamRoleRequirementDto("Senior Java Developer", "Java", ProficiencyLevel.Advanced),
            new TeamRoleRequirementDto("DevOps Engineer", "DevOps", ProficiencyLevel.Advanced)
        ]);

        var result = await service.MatchOrganizationTeamAsync(2, request);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.FilledCount);
        Assert.Equal(2, result.Value.TotalRoles);
        var devOpsRole = result.Value.RoleResults.Single(role => role.RoleTitle == "DevOps Engineer");
        Assert.False(devOpsRole.IsFilled);
        Assert.Equal(TeamRoleGapType.SkillGap, devOpsRole.GapType);
    }

    [Fact]
    public async Task MatchOrganizationTeamAsync_Fails_WhenRolesMissing()
    {
        await using var dbContext = CreateDbContext();
        await SeedAiScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.MatchOrganizationTeamAsync(2, new AiTeamMatchRequest([]));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminResultCode.ValidationError, result.Code);
    }

    private static async Task SeedTeamMatchScenarioAsync(ApplicationDbContext dbContext)
    {
        SchemaV3TestHelpers.SeedUser(dbContext, 2, "manager", "Manager", "m@test", UserRole.Manager);
        SchemaV3TestHelpers.SeedUser(dbContext, 20, "java1", "Java Developer", "java@test", UserRole.Employee);
        SchemaV3TestHelpers.SeedUser(dbContext, 21, "qa1", "QA Engineer", "qa@test", UserRole.Employee);
        await dbContext.SaveChangesAsync();

        var javaSkill = new Skill { Id = 10, Name = "Java", Category = SkillCategory.Backend };
        var qaSkill = new Skill { Id = 11, Name = "QA Testing", Category = SkillCategory.QA };
        dbContext.Skills.AddRange(javaSkill, qaSkill);

        dbContext.UserSkills.AddRange(
            new UserSkill { UserId = 20, SkillId = 10, ProficiencyLevel = ProficiencyLevel.Advanced, Skill = javaSkill },
            new UserSkill { UserId = 21, SkillId = 11, ProficiencyLevel = ProficiencyLevel.Intermediate, Skill = qaSkill });

        var javaProfile = dbContext.UserProfiles.Single(profile => profile.UserId == 20);
        javaProfile.CurrentUtilizationPercent = 0;
        javaProfile.ResourceStatus = EmployeeStatus.Bench;

        var qaProfile = dbContext.UserProfiles.Single(profile => profile.UserId == 21);
        qaProfile.CurrentUtilizationPercent = 0;
        qaProfile.ResourceStatus = EmployeeStatus.Bench;

        await dbContext.SaveChangesAsync();
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

    [Fact]
    public async Task SummarizeProjectRiskAsync_UsesLlm_WhenProviderConfigured()
    {
        await using var dbContext = CreateDbContext();
        await SeedAiScenarioAsync(dbContext);
        dbContext.SystemConfigurations.AddRange(
            new Server.Models.SystemConfiguration { Id = 201, Key = "LlmProvider", Value = "Gemma", Description = "test" },
            new Server.Models.SystemConfiguration { Id = 202, Key = "LlmApiKey", Value = "secret", Description = "test" });
        await dbContext.SaveChangesAsync();

        var fakeClient = new FakeLlmCompletionClient(LlmProvider.Gemma, "Plain English risk summary from Gemma.");
        var service = CreateService(dbContext, fakeClient);
        var result = await service.SummarizeProjectRiskAsync(2, new AiProjectRiskSummaryRequest(1));

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.UsedFallback);
        Assert.Equal(LlmProvider.Gemma, result.Value.ProviderUsed);
        Assert.Contains("Gemma", result.Value.Summary, StringComparison.OrdinalIgnoreCase);
    }

    private static AiAssistantService CreateService(
        ApplicationDbContext dbContext,
        ILlmCompletionClient? llmClient = null)
    {
        var clients = llmClient is null
            ? Array.Empty<ILlmCompletionClient>()
            : new ILlmCompletionClient[] { llmClient };

        return new AiAssistantService(
            new UserProfileRepository(dbContext),
            new ProjectRepository(dbContext),
            new AllocationRepository(dbContext),
            new SkillMatchCandidateFilter(),
            new OrganizationTeamMatcher(),
            new ProjectRiskFactAssembler(
                new ProjectRepository(dbContext),
                new AllocationRepository(dbContext),
                new TimesheetRepository(dbContext),
                new SystemConfigurationRepository(dbContext)),
            new SkillMatchPromptBuilder(),
            new ProjectRiskPromptBuilder(),
            new TeamMatchPromptBuilder(),
            new DeterministicSkillMatchSummarizer(),
            new DeterministicProjectRiskSummarizer(),
            new DeterministicTeamMatchSummarizer(),
            new LlmConfigurationReader(new SystemConfigurationRepository(dbContext)),
            new LlmCompletionClientFactory(clients));
    }

    private sealed class FakeLlmCompletionClient(LlmProvider provider, string responseText) : ILlmCompletionClient
    {
        public LlmProvider Provider { get; } = provider;

        public Task<LlmCompletionResult> CompleteAsync(
            LlmCompletionRequest request,
            LlmSettings settings,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LlmCompletionResult.Success(responseText));
        }
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
