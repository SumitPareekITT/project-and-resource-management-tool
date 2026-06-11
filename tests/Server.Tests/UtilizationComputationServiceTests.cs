using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class UtilizationComputationServiceTests
{
    [Fact]
    public async Task SyncAllActiveProfilesAsync_ResetsUtilization_WhenAllocationEnded()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SchemaV3TestHelpers.SeedUser(dbContext, 2, "manager", "Manager", "m@test", UserRole.Manager);
        SchemaV3TestHelpers.SeedUser(dbContext, 20, "emp20", "Employee One", "e1@test", UserRole.Employee);
        await dbContext.SaveChangesAsync();
        var trackedProfile = dbContext.UserProfiles.Single(p => p.UserId == 20);
        trackedProfile.ManagerUserId = 2;
        trackedProfile.CurrentUtilizationPercent = 80;
        trackedProfile.ResourceStatus = EmployeeStatus.PartiallyAllocated;
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerUserId = 2,
            StartDate = today.AddMonths(-1),
            EndDate = today.AddMonths(6),
            Status = ProjectStatus.Active
        });
        dbContext.Allocations.Add(new Allocation
        {
            Id = 1,
            UserId = 20,
            ProjectId = 1,
            CreatedByUserId = 2,
            UtilizationPercentage = 80,
            FromDate = today.AddDays(-30),
            ToDate = today.AddDays(-1),
            Status = AllocationStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var updatedCount = await service.SyncAllActiveProfilesAsync();

        var profile = await dbContext.UserProfiles.SingleAsync(item => item.UserId == 20);
        Assert.Equal(2, updatedCount);
        Assert.Equal(0, profile.CurrentUtilizationPercent);
        Assert.Equal(EmployeeStatus.Bench, profile.ResourceStatus);
    }

    private static UtilizationComputationService CreateService(ApplicationDbContext dbContext)
    {
        return new UtilizationComputationService(new UserProfileRepository(dbContext), new AllocationRepository(dbContext));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
