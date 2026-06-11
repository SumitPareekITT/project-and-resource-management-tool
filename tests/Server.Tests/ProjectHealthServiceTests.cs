using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class ProjectHealthServiceTests
{
    [Fact]
    public async Task EvaluateAndPersistAllProjectsAsync_SetsAtRisk_ForOverdueMilestone()
    {
        await using var dbContext = CreateDbContext();
        SeedManagerUser(dbContext);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = CreateActiveProject(today);
        project.Milestones.Add(new Milestone
        {
            Id = 1,
            ProjectId = 1,
            Title = "Release",
            DueDate = today.AddDays(-2),
            Status = MilestoneStatus.InProgress
        });
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.EvaluateAndPersistAllProjectsAsync();

        var updated = await dbContext.Projects.SingleAsync(item => item.Id == 1);
        Assert.Equal(ProjectHealthStatus.AtRisk, updated.HealthStatus);
    }

    [Fact]
    public async Task EvaluateAndPersistAllProjectsAsync_SetsAttention_ForMilestoneDueSoon()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedManagerUser(dbContext);
        var project = CreateActiveProject(today);
        project.Status = ProjectStatus.Planned;
        project.Milestones.Add(new Milestone
        {
            Id = 1,
            ProjectId = 1,
            Title = "Sprint Close",
            DueDate = today.AddDays(3),
            Status = MilestoneStatus.InProgress
        });
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.EvaluateAndPersistAllProjectsAsync();

        var updated = await dbContext.Projects.SingleAsync(item => item.Id == 1);
        Assert.Equal(ProjectHealthStatus.Attention, updated.HealthStatus);
    }

    [Fact]
    public async Task EvaluateAndPersistAllProjectsAsync_SetsAtRisk_WhenActiveProjectHasNoAllocations()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = CreateActiveProject(today);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.EvaluateAndPersistAllProjectsAsync();

        var updated = await dbContext.Projects.SingleAsync(item => item.Id == 1);
        Assert.Equal(ProjectHealthStatus.AtRisk, updated.HealthStatus);
    }

    [Fact]
    public async Task ListManagerProjectHealthAsync_ReturnsSignalsForOwnedProjects()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedManagerUser(dbContext);
        var project = CreateActiveProject(today);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.ListManagerProjectHealthAsync(managerUserId: 2);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal(ProjectHealthStatus.AtRisk, result.Value![0].HealthStatus);
        Assert.Contains(result.Value[0].HealthSignals, signal => signal.Contains("no current team allocations", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAndPersistAllProjectsAsync_SetsOnTrack_WhenNoRiskSignals()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = CreateActiveProject(today);
        project.TotalStoryPoints = 100;
        project.CompletedStoryPoints = 50;
        project.StartDate = today.AddDays(-50);
        project.EndDate = today.AddDays(50);
        project.Milestones.Add(new Milestone
        {
            Id = 1,
            ProjectId = 1,
            Title = "Future Milestone",
            DueDate = today.AddDays(20),
            Status = MilestoneStatus.InProgress
        });
        dbContext.Projects.Add(project);
        SchemaV3TestHelpers.SeedUser(dbContext, 20, "emp20", "Employee One", "e1@test", UserRole.Employee);
        await dbContext.SaveChangesAsync();
        dbContext.UserProfiles.Single(p => p.UserId == 20).ManagerUserId = 2;
dbContext.Allocations.Add(new Allocation
        {
            Id = 1,
            UserId = 20,
            ProjectId = 1,
            CreatedByUserId = 2,
            UtilizationPercentage = 50,
            FromDate = today.AddDays(-14),
            Status = AllocationStatus.Active
        });

        var previousWeek = GetPreviousWeekStart(today);
        dbContext.Timesheets.Add(new Timesheet
        {
            Id = 1,
            UserId = 20,
            WeekStartDate = previousWeek,
            TotalHours = 20,
            Status = TimesheetStatus.Submitted
        });
        dbContext.TimesheetEntries.Add(new TimesheetEntry
        {
            Id = 1,
            TimesheetId = 1,
            ProjectId = 1,
            HoursWorked = 20
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.EvaluateAndPersistAllProjectsAsync();

        var updated = await dbContext.Projects.SingleAsync(item => item.Id == 1);
        Assert.Equal(ProjectHealthStatus.OnTrack, updated.HealthStatus);
    }

    private static void SeedManagerUser(ApplicationDbContext dbContext)
    {
        SchemaV3TestHelpers.SeedUser(dbContext, 2, "manager", "Manager", "m@test", UserRole.Manager);
    }

    private static Project CreateActiveProject(DateOnly today)
    {
        return new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerUserId = 2,
            StartDate = today.AddMonths(-1),
            EndDate = today.AddMonths(6),
            Status = ProjectStatus.Active
        };
    }

    private static DateOnly GetPreviousWeekStart(DateOnly referenceDate)
    {
        var daysFromMonday = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return referenceDate.AddDays(-daysFromMonday - 7);
    }

    private static ProjectHealthService CreateService(ApplicationDbContext dbContext)
    {
        return new ProjectHealthService(
            new ProjectRepository(dbContext),
            new AllocationRepository(dbContext),
            new TimesheetRepository(dbContext),
            new SystemConfigurationRepository(dbContext));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
