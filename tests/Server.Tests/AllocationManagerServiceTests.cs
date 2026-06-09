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
        var result = await service.AllocateAsync(2, new CreateAllocationRequest(1, 99, 50, DateOnly.FromDateTime(DateTime.UtcNow), null)); // employee 99 reports to manager 3

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
            EmployeeId = 10,
            ProjectId = 1,
            CreatedByManagerId = 2,
            UtilizationPercentage = 80,
            FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Status = AllocationStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.AllocateAsync(2, new CreateAllocationRequest(1, 10, 30, DateOnly.FromDateTime(DateTime.UtcNow), null));

        Assert.False(result.Succeeded);
        Assert.Contains("100%", result.Message);
    }

    [Fact]
    public async Task AllocateAsync_Succeeds_ForDirectTeamMember()
    {
        await using var dbContext = CreateDbContext();
        await SeedTeamDataAsync(dbContext);

        var service = CreateService(dbContext);
        var result = await service.AllocateAsync(2, new CreateAllocationRequest(1, 10, 50, DateOnly.FromDateTime(DateTime.UtcNow), null));

        Assert.True(result.Succeeded);
        var employee = await dbContext.Employees.SingleAsync(item => item.Id == 10);
        Assert.Equal(50, employee.CurrentUtilizationPercent);
        Assert.Equal(EmployeeStatus.PartiallyAllocated, employee.Status);
    }

    private static async Task SeedTeamDataAsync(ApplicationDbContext dbContext)
    {
        dbContext.Users.AddRange(
            new User { Id = 2, FullName = "Manager", Email = "m@test", Username = "manager", PasswordHash = "h", Role = UserRole.Manager, IsActive = true },
            new User { Id = 3, FullName = "Other Manager", Email = "om@test", Username = "omanager", PasswordHash = "h", Role = UserRole.Manager, IsActive = true },
            new User { Id = 20, FullName = "Team Member One", Email = "one@test", Username = "one", PasswordHash = "h", Role = UserRole.Employee, IsActive = true },
            new User { Id = 21, FullName = "Team Member Two", Email = "two@test", Username = "two", PasswordHash = "h", Role = UserRole.Employee, IsActive = true },
            new User { Id = 29, FullName = "Other Team Member", Email = "other@test", Username = "other", PasswordHash = "h", Role = UserRole.Employee, IsActive = true });

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))
        });

        dbContext.Employees.AddRange(
            new Employee { Id = 10, UserId = 20, ManagerId = 2, FullName = "Team Member One", Email = "one@test", Department = "Eng", Designation = "SE" },
            new Employee { Id = 11, UserId = 21, ManagerId = 2, FullName = "Team Member Two", Email = "two@test", Department = "Eng", Designation = "SE" },
            new Employee { Id = 99, UserId = 29, ManagerId = 3, FullName = "Other Team Member", Email = "other@test", Department = "Eng", Designation = "SE" });

        await dbContext.SaveChangesAsync();
    }

    private static AllocationManagerService CreateService(ApplicationDbContext dbContext)
    {
        return new AllocationManagerService(
            new EmployeeRepository(dbContext),
            new ProjectRepository(dbContext),
            new AllocationRepository(dbContext),
            new UtilizationComputationService(
                new EmployeeRepository(dbContext),
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
