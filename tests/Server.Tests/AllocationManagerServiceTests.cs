using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Manager;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Shared.DTOs.Manager;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class AllocationManagerServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsOnlyDirectTeam()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamDataAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.GetDashboardAsync(managerUserId: 2);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, row => row.FullName == "Team Member One");
        Assert.DoesNotContain(result.Value, row => row.FullName == "Other Team Member");
    }

    [Fact]
    public async Task AllocateAsync_Fails_WhenEmployeeNotInDirectTeam()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamDataAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.AllocateAsync(2, new CreateAllocationRequest(1, 29, 50, DateOnly.FromDateTime(DateTime.UtcNow), null)); // employee 99 reports to manager 3

        Assert.False(result.Succeeded);
        Assert.Equal(AdminResultCode.ValidationError, result.Code);
    }

    [Fact]
    public async Task AllocateAsync_Fails_WhenCapacityExceeded()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamDataAsync(dbContext);
        dbContext.Allocations.Add(new Allocation
        {
            Id = 50,
            UserId = 20,
            ProjectId = 1,
            CreatedByUserId = 2,
            UtilizationPercentage = 80,
            FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Status = AllocationStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.AllocateAsync(2, new CreateAllocationRequest(1, 20, 30, DateOnly.FromDateTime(DateTime.UtcNow), null));

        Assert.False(result.Succeeded);
        Assert.Contains("100%", result.Message);
    }

    [Fact]
    public async Task AllocateAsync_Succeeds_ForDirectTeamMember()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamDataAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.AllocateAsync(2, new CreateAllocationRequest(1, 20, 50, DateOnly.FromDateTime(DateTime.UtcNow), null));

        Assert.True(result.Succeeded);
        var employee = await dbContext.UserProfiles.SingleAsync(item => item.UserId == 20);
        Assert.Equal(50, employee.CurrentUtilizationPercent);
        Assert.Equal(EmployeeStatus.PartiallyAllocated, employee.ResourceStatus);
    }

        private static async Task SeedTeamDataAsync(ApplicationDbContext dbContext)
    {
        SchemaV3TestHelpers.SeedUser(dbContext, 2, "manager", "Manager", "m@test", UserRole.Manager);
        SchemaV3TestHelpers.SeedUser(dbContext, 3, "omanager", "Other Manager", "om@test", UserRole.Manager);
        SchemaV3TestHelpers.SeedUser(dbContext, 20, "one", "Team Member One", "one@test", UserRole.Employee);
        SchemaV3TestHelpers.SeedUser(dbContext, 21, "two", "Team Member Two", "two@test", UserRole.Employee);
        SchemaV3TestHelpers.SeedUser(dbContext, 29, "other", "Other Team Member", "other@test", UserRole.Employee);

        await dbContext.SaveChangesAsync();

        dbContext.UserProfiles.Single(p => p.UserId == 20).ManagerUserId = 2;
        dbContext.UserProfiles.Single(p => p.UserId == 21).ManagerUserId = 2;
        dbContext.UserProfiles.Single(p => p.UserId == 29).ManagerUserId = 3;

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerUserId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))
        });

        await dbContext.SaveChangesAsync();
    }

    private static AllocationManagerService CreateService(ApplicationDbContext dbContext)
    {
        return new AllocationManagerService(
            new UserProfileRepository(dbContext),
            new ProjectRepository(dbContext),
            new AllocationRepository(dbContext),
            new UtilizationComputationService(
                new UserProfileRepository(dbContext),
                new AllocationRepository(dbContext)));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
