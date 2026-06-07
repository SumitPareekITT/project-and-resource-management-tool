using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class EmployeeAdminServiceTests
{
    [Fact]
    public async Task AssignManagerAsync_AssignsManager_WhenEmployeeProfileExists()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        dbContext.Employees.Add(new Employee
        {
            Id = 1,
            UserId = 3,
            FullName = "Developer One",
            Email = "dev1@local.test",
            Department = "Engineering",
            Designation = "Software Engineer"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.AssignManagerAsync(new AssignManagerRequest(EmployeeUserId: 3, ManagerUserId: 2));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.ManagerId);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_DeactivatesUserAndEndsActiveAllocations()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
        });
        dbContext.Employees.Add(new Employee
        {
            Id = 10,
            UserId = 3,
            FullName = "Developer One",
            Email = "dev1@local.test",
            Department = "Engineering",
            Designation = "Software Engineer",
            CurrentUtilizationPercent = 80
        });
        dbContext.Allocations.Add(new Allocation
        {
            Id = 100,
            EmployeeId = 10,
            ProjectId = 1,
            CreatedByManagerId = 2,
            UtilizationPercentage = 80,
            FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            ToDate = null,
            Status = AllocationStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.DeactivateEmployeeAsync(10);

        Assert.True(result.Succeeded);
        var employee = await dbContext.Employees.Include(item => item.User).SingleAsync(item => item.Id == 10);
        var allocation = await dbContext.Allocations.SingleAsync(item => item.Id == 100);

        Assert.False(employee.IsActive);
        Assert.Equal(EmployeeStatus.Inactive, employee.Status);
        Assert.False(employee.User.IsActive);
        Assert.Equal(AllocationStatus.Ended, allocation.Status);
        Assert.NotNull(allocation.ToDate);
    }

    [Fact]
    public async Task UpsertEmployeeSkillAsync_AddsSkillMapping_WhenSkillIsValid()
    {
        await using var dbContext = CreateDbContext();
        SeedUsers(dbContext);
        dbContext.Employees.Add(new Employee
        {
            Id = 22,
            UserId = 3,
            FullName = "Developer One",
            Email = "dev1@local.test",
            Department = "Engineering",
            Designation = "Software Engineer"
        });
        dbContext.Skills.Add(new Skill
        {
            Id = 5,
            Name = "C#",
            Category = SkillCategory.Backend,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.UpsertEmployeeSkillAsync(
            22,
            new UpsertEmployeeSkillRequest(5, ProficiencyLevel.Advanced, 4.5m, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.True(result.Succeeded);
        var mapping = await dbContext.EmployeeSkills.SingleAsync(item => item.EmployeeId == 22 && item.SkillId == 5);
        Assert.Equal(ProficiencyLevel.Advanced, mapping.ProficiencyLevel);
        Assert.Equal(4.5m, mapping.YearsOfExperience);
    }

    private static EmployeeAdminService CreateService(ApplicationDbContext dbContext)
    {
        return new EmployeeAdminService(
            new EmployeeRepository(dbContext),
            new UserRepository(dbContext),
            new SkillRepository(dbContext),
            new AllocationRepository(dbContext));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SeedUsers(ApplicationDbContext dbContext)
    {
        dbContext.Users.AddRange(
            new User
            {
                Id = 1,
                FullName = "Admin User",
                Email = "admin@local.test",
                Username = "admin",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true
            },
            new User
            {
                Id = 2,
                FullName = "Manager User",
                Email = "manager@local.test",
                Username = "manager",
                PasswordHash = "hash",
                Role = UserRole.Manager,
                IsActive = true
            },
            new User
            {
                Id = 3,
                FullName = "Employee User",
                Email = "employee@local.test",
                Username = "employee",
                PasswordHash = "hash",
                Role = UserRole.Employee,
                IsActive = true
            });
    }
}
