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
    public async Task SyncAllActiveEmployeesAsync_ResetsUtilization_WhenAllocationEnded()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        dbContext.Users.Add(new User
        {
            Id = 2,
            FullName = "Manager",
            Email = "m@test",
            Username = "manager",
            PasswordHash = "h",
            Role = UserRole.Manager,
            IsActive = true
        });

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerId = 2,
            StartDate = today.AddMonths(-1),
            EndDate = today.AddMonths(6),
            Status = ProjectStatus.Active
        });

        dbContext.Employees.Add(new Employee
        {
            Id = 10,
            UserId = 20,
            ManagerId = 2,
            FullName = "Employee One",
            Email = "e1@test",
            Department = "Eng",
            Designation = "SE",
            IsActive = true,
            CurrentUtilizationPercent = 80,
            Status = EmployeeStatus.PartiallyAllocated
        });

        dbContext.Allocations.Add(new Allocation
        {
            Id = 1,
            EmployeeId = 10,
            ProjectId = 1,
            CreatedByManagerId = 2,
            UtilizationPercentage = 80,
            FromDate = today.AddDays(-30),
            ToDate = today.AddDays(-1),
            Status = AllocationStatus.Active
        });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var updatedCount = await service.SyncAllActiveEmployeesAsync();

        var employee = await dbContext.Employees.SingleAsync(item => item.Id == 10);
        Assert.Equal(1, updatedCount);
        Assert.Equal(0, employee.CurrentUtilizationPercent);
        Assert.Equal(EmployeeStatus.Bench, employee.Status);
    }

    [Fact]
    public async Task SyncEmployeeAsync_UsesOnlyCurrentDateAllocations()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        dbContext.Users.Add(new User
        {
            Id = 2,
            FullName = "Manager",
            Email = "m@test",
            Username = "manager",
            PasswordHash = "h",
            Role = UserRole.Manager,
            IsActive = true
        });

        dbContext.Projects.AddRange(
            new Project
            {
                Id = 1,
                Name = "Apollo",
                ManagerId = 2,
                StartDate = today.AddMonths(-1),
                EndDate = today.AddMonths(6),
                Status = ProjectStatus.Active
            },
            new Project
            {
                Id = 2,
                Name = "Beta",
                ManagerId = 2,
                StartDate = today.AddMonths(-1),
                EndDate = today.AddMonths(6),
                Status = ProjectStatus.Active
            });

        var employee = new Employee
        {
            Id = 10,
            UserId = 20,
            ManagerId = 2,
            FullName = "Employee One",
            Email = "e1@test",
            Department = "Eng",
            Designation = "SE",
            IsActive = true
        };
        dbContext.Employees.Add(employee);

        dbContext.Allocations.AddRange(
            new Allocation
            {
                Id = 1,
                EmployeeId = 10,
                ProjectId = 1,
                CreatedByManagerId = 2,
                UtilizationPercentage = 60,
                FromDate = today.AddDays(-5),
                Status = AllocationStatus.Active
            },
            new Allocation
            {
                Id = 2,
                EmployeeId = 10,
                ProjectId = 2,
                CreatedByManagerId = 2,
                UtilizationPercentage = 40,
                FromDate = today.AddDays(5),
                Status = AllocationStatus.Active
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.SyncEmployeeAsync(employee);
        await dbContext.SaveChangesAsync();

        Assert.Equal(60, employee.CurrentUtilizationPercent);
        Assert.Equal(EmployeeStatus.PartiallyAllocated, employee.Status);
    }

    private static UtilizationComputationService CreateService(ApplicationDbContext dbContext)
    {
        return new UtilizationComputationService(
            new EmployeeRepository(dbContext),
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
