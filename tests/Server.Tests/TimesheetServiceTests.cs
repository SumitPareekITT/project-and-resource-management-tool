using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Timesheets;
using ProjectResourceManagement.Shared.DTOs.Timesheet;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class TimesheetServiceTests
{
    [Fact]
    public async Task SubmitAsync_Succeeds_WithValidAllocation()
    {
        await using var dbContext = CreateDbContext();
        await SeedTimesheetScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var weekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var result = await service.SubmitAsync(30, new SubmitTimesheetRequest(
            weekStart,
            [new SubmitTimesheetEntryRequest(1, 20, "API work", [])]));

        Assert.True(result.Succeeded);
        Assert.Equal(20, result.Value!.TotalHours);
    }

    [Fact]
    public async Task SubmitAsync_Fails_ForDuplicateWeek()
    {
        await using var dbContext = CreateDbContext();
        await SeedTimesheetScenarioAsync(dbContext);
        var weekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));

        dbContext.Timesheets.Add(new Timesheet
        {
            Id = 9,
            EmployeeId = 10,
            WeekStartDate = weekStart,
            TotalHours = 8,
            Status = TimesheetStatus.Submitted
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.SubmitAsync(30, new SubmitTimesheetRequest(
            weekStart,
            [new SubmitTimesheetEntryRequest(1, 8, "", [])]));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminResultCode.Conflict, result.Code);
    }

    [Fact]
    public async Task SubmitAsync_Fails_ForFutureWeek()
    {
        await using var dbContext = CreateDbContext();
        await SeedTimesheetScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var futureWeek = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(7);
        var result = await service.SubmitAsync(30, new SubmitTimesheetRequest(
            futureWeek,
            [new SubmitTimesheetEntryRequest(1, 8, "", [])]));

        Assert.False(result.Succeeded);
        Assert.Contains("Future-week", result.Message);
    }

    [Fact]
    public async Task SubmitAsync_Fails_WhenProjectHoursExceedAllocationCap()
    {
        await using var dbContext = CreateDbContext();
        await SeedTimesheetScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var weekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var result = await service.SubmitAsync(30, new SubmitTimesheetRequest(
            weekStart,
            [new SubmitTimesheetEntryRequest(1, 25, "", [])])); // 50% of 40h = max 20h

        Assert.False(result.Succeeded);
        Assert.Contains("allocation limit", result.Message);
    }

    [Fact]
    public async Task GetMissingTimesheetRemindersAsync_ReturnsEmployeesWithoutSubmission()
    {
        await using var dbContext = CreateDbContext();
        await SeedTimesheetScenarioAsync(dbContext);

        var service = CreateService(dbContext);
        var previousWeek = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(-7);
        var result = await service.GetMissingTimesheetRemindersAsync(2, previousWeek);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal("Employee One", result.Value![0].EmployeeName);
    }

    private static async Task SeedTimesheetScenarioAsync(ApplicationDbContext dbContext)
    {
        dbContext.Users.AddRange(
            new User { Id = 2, FullName = "Manager", Email = "m@test", Username = "manager", PasswordHash = "h", Role = UserRole.Manager, IsActive = true },
            new User { Id = 30, FullName = "Employee One", Email = "e1@test", Username = "emp1", PasswordHash = "h", Role = UserRole.Employee, IsActive = true });

        dbContext.Projects.Add(new Project
        {
            Id = 1,
            Name = "Apollo",
            ManagerId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6))
        });

        dbContext.Employees.Add(new Employee
        {
            Id = 10,
            UserId = 30,
            ManagerId = 2,
            FullName = "Employee One",
            Email = "e1@test",
            Department = "Eng",
            Designation = "SE",
            IsActive = true
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

    private static TimesheetService CreateService(ApplicationDbContext dbContext)
    {
        return new TimesheetService(
            new EmployeeRepository(dbContext),
            new AllocationRepository(dbContext),
            new TimesheetRepository(dbContext),
            new ActivityTagRepository(dbContext),
            new SystemConfigurationRepository(dbContext));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }
}
