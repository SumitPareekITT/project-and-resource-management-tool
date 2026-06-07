using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class ProjectAdminServiceTests
{
    [Fact]
    public async Task CreateProjectAsync_Succeeds_WithValidManager()
    {
        await using var dbContext = CreateDbContext();
        await SeedManagerAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateProjectAsync(new CreateProjectRequest(
            "Apollo",
            "Acme Corp",
            "Core platform",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            2,
            100));

        Assert.True(result.Succeeded);
        Assert.Equal("Apollo", result.Value!.Name);
        Assert.Equal("0/100", result.Value.StoryPointProgress);
    }

    [Fact]
    public async Task AddMilestoneAsync_UpdatesProjectStoryPointTotals()
    {
        await using var dbContext = CreateDbContext();
        await SeedManagerAsync(dbContext);
        dbContext.Projects.Add(new Project
        {
            Id = 10,
            Name = "Apollo",
            ManagerId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            TotalStoryPoints = 0,
            CompletedStoryPoints = 0
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.AddMilestoneAsync(10, new UpsertMilestoneRequest(
            "MVP",
            "Initial release",
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            MilestoneStatus.InProgress,
            40,
            10));

        Assert.True(result.Succeeded);
        var project = await dbContext.Projects.Include(item => item.Milestones).SingleAsync(item => item.Id == 10);
        Assert.Equal(40, project.TotalStoryPoints);
        Assert.Equal(10, project.CompletedStoryPoints);
    }

    [Fact]
    public async Task GetAllocationMatrixAsync_ReturnsActiveAllocations()
    {
        await using var dbContext = CreateDbContext();
        await SeedManagerAsync(dbContext);
        dbContext.Employees.Add(new Employee
        {
            Id = 1,
            UserId = 3,
            FullName = "Dev One",
            Email = "dev@test.local",
            Department = "Eng",
            Designation = "SE"
        });
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))
        });
        dbContext.Allocations.Add(new Allocation
        {
            Id = 1,
            EmployeeId = 1,
            ProjectId = 1,
            CreatedByManagerId = 2,
            UtilizationPercentage = 50,
            FromDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = AllocationStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetAllocationMatrixAsync();

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal("Apollo", result.Value![0].ProjectName);
    }

    private static async Task SeedManagerAsync(ApplicationDbContext dbContext)
    {
        dbContext.Users.AddRange(
            new User
            {
                Id = 2,
                FullName = "Manager",
                Email = "manager@test.local",
                Username = "manager",
                PasswordHash = "hash",
                Role = UserRole.Manager,
                IsActive = true
            },
            new User
            {
                Id = 3,
                FullName = "Employee",
                Email = "employee@test.local",
                Username = "employee",
                PasswordHash = "hash",
                Role = UserRole.Employee,
                IsActive = true
            });
        await dbContext.SaveChangesAsync();
    }

    private static ProjectAdminService CreateService(ApplicationDbContext dbContext)
    {
        return new ProjectAdminService(
            new ProjectRepository(dbContext),
            new MilestoneRepository(dbContext),
            new UserRepository(dbContext),
            new AllocationRepository(dbContext));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
