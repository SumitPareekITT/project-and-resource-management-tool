using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Timesheet;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Timesheets;

public sealed class TimesheetService(
    EmployeeRepository employeeRepository,
    AllocationRepository allocationRepository,
    TimesheetRepository timesheetRepository,
    ActivityTagRepository activityTagRepository,
    SystemConfigurationRepository systemConfigurationRepository)
{
    public async Task<AdminResult<IReadOnlyList<ActiveProjectForTimesheetDto>>> GetActiveProjectsForWeekAsync(
        int employeeUserId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdAsync(employeeUserId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<IReadOnlyList<ActiveProjectForTimesheetDto>>.Fail(AdminResultCode.NotFound, "Employee profile was not found.");
        }

        var normalizedWeek = NormalizeWeekStart(weekStartDate);
        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var allocations = await allocationRepository.ListActiveByEmployeeAsync(employee.Id, cancellationToken);
        var activeForWeek = allocations
            .Where(allocation => AllocationCoversWeek(allocation, normalizedWeek))
            .Select(allocation => new ActiveProjectForTimesheetDto(
                allocation.ProjectId,
                allocation.Project.Name,
                allocation.UtilizationPercentage,
                Math.Round(maxWeeklyHours * allocation.UtilizationPercentage / BusinessRules.FullAllocationPercent, 2)))
            .ToList();

        return AdminResult<IReadOnlyList<ActiveProjectForTimesheetDto>>.Success(activeForWeek);
    }

    public async Task<AdminResult<TimesheetDetailDto>> SubmitAsync(
        int employeeUserId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Entries.Count == 0)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "At least one timesheet entry is required.");
        }

        var employee = await GetEmployeeByUserIdAsync(employeeUserId, cancellationToken);
        if (employee is null || !employee.IsActive)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "Employee profile was not found or is inactive.");
        }

        var weekStart = NormalizeWeekStart(request.WeekStartDate);
        if (weekStart != request.WeekStartDate)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "Week start date must be a Monday.");
        }

        var currentWeekStart = NormalizeWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        if (weekStart > currentWeekStart)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "Future-week timesheet submission is not allowed.");
        }

        if (await timesheetRepository.ExistsForEmployeeWeekAsync(employee.Id, weekStart, cancellationToken))
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.Conflict, "Timesheet for this week already exists.");
        }

        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var totalHours = request.Entries.Sum(entry => entry.HoursWorked);
        if (totalHours > maxWeeklyHours)
        {
            return AdminResult<TimesheetDetailDto>.Fail(
                AdminResultCode.ValidationError,
                $"Total weekly hours cannot exceed {maxWeeklyHours}.");
        }

        var allocations = await allocationRepository.ListActiveByEmployeeAsync(employee.Id, cancellationToken);
        var weekAllocations = allocations.Where(allocation => AllocationCoversWeek(allocation, weekStart)).ToList();

        var timesheet = new Timesheet
        {
            EmployeeId = employee.Id,
            WeekStartDate = weekStart,
            TotalHours = totalHours,
            Status = TimesheetStatus.Submitted,
            SubmittedAtUtc = DateTime.UtcNow
        };

        foreach (var entryRequest in request.Entries)
        {
            if (entryRequest.HoursWorked <= 0)
            {
                return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "Hours worked must be greater than zero.");
            }

            var allocation = weekAllocations.FirstOrDefault(item => item.ProjectId == entryRequest.ProjectId);
            if (allocation is null)
            {
                return AdminResult<TimesheetDetailDto>.Fail(
                    AdminResultCode.ValidationError,
                    $"No active allocation found for project {entryRequest.ProjectId} in the selected week.");
            }

            var projectMaxHours = maxWeeklyHours * allocation.UtilizationPercentage / BusinessRules.FullAllocationPercent;
            if (entryRequest.HoursWorked > projectMaxHours)
            {
                return AdminResult<TimesheetDetailDto>.Fail(
                    AdminResultCode.ValidationError,
                    $"Hours for project {entryRequest.ProjectId} exceed allocation limit of {projectMaxHours:0.##} hours.");
            }

            var tags = entryRequest.ActivityTagIds.Count == 0
                ? []
                : await activityTagRepository.ListByIdsAsync(entryRequest.ActivityTagIds, cancellationToken);

            if (entryRequest.ActivityTagIds.Count != tags.Count)
            {
                return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "One or more activity tags are invalid.");
            }

            var entry = new TimesheetEntry
            {
                ProjectId = entryRequest.ProjectId,
                HoursWorked = entryRequest.HoursWorked,
                Notes = entryRequest.Notes.Trim()
            };

            foreach (var tag in tags)
            {
                entry.ActivityTags.Add(tag);
            }

            timesheet.Entries.Add(entry);
        }

        await timesheetRepository.AddAsync(timesheet, cancellationToken);
        await timesheetRepository.SaveChangesAsync(cancellationToken);

        var created = await timesheetRepository.GetByIdAsync(timesheet.Id, cancellationToken);
        return AdminResult<TimesheetDetailDto>.Success(MapDetail(created!));
    }

    public async Task<AdminResult<IReadOnlyList<TimesheetSummaryDto>>> ListEmployeeHistoryAsync(
        int employeeUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdAsync(employeeUserId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<IReadOnlyList<TimesheetSummaryDto>>.Fail(AdminResultCode.NotFound, "Employee profile was not found.");
        }

        var timesheets = await timesheetRepository.ListByEmployeeAsync(employee.Id, cancellationToken);
        return AdminResult<IReadOnlyList<TimesheetSummaryDto>>.Success(timesheets.Select(MapSummary).ToList());
    }

    public async Task<AdminResult<TimesheetDetailDto>> GetEmployeeTimesheetAsync(
        int employeeUserId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdAsync(employeeUserId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "Employee profile was not found.");
        }

        var timesheet = await timesheetRepository.GetByEmployeeWeekAsync(employee.Id, NormalizeWeekStart(weekStartDate), cancellationToken);
        if (timesheet is null)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "Timesheet was not found.");
        }

        return AdminResult<TimesheetDetailDto>.Success(MapDetail(timesheet));
    }

    public async Task<AdminResult<IReadOnlyList<EmployeeAllocationDto>>> ListEmployeeAllocationsAsync(
        int employeeUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdAsync(employeeUserId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<IReadOnlyList<EmployeeAllocationDto>>.Fail(AdminResultCode.NotFound, "Employee profile was not found.");
        }

        var allocations = await allocationRepository.ListActiveByEmployeeAsync(employee.Id, cancellationToken);
        var mapped = allocations.Select(allocation => new EmployeeAllocationDto(
            allocation.Id,
            allocation.ProjectId,
            allocation.Project.Name,
            allocation.UtilizationPercentage,
            allocation.FromDate,
            allocation.ToDate,
            allocation.Status)).ToList();

        return AdminResult<IReadOnlyList<EmployeeAllocationDto>>.Success(mapped);
    }

    public async Task<AdminResult<IReadOnlyList<TimesheetSummaryDto>>> ListManagerTeamTimesheetsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var timesheets = await timesheetRepository.ListByManagerTeamAsync(managerUserId, cancellationToken);
        return AdminResult<IReadOnlyList<TimesheetSummaryDto>>.Success(timesheets.Select(MapSummary).ToList());
    }

    public async Task<AdminResult<TimesheetDetailDto>> GetManagerTeamTimesheetAsync(
        int managerUserId,
        int timesheetId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await timesheetRepository.GetByIdAsync(timesheetId, cancellationToken);
        if (timesheet is null)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "Timesheet was not found.");
        }

        if (timesheet.Employee.ManagerId != managerUserId)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "Timesheet is outside your direct team.");
        }

        return AdminResult<TimesheetDetailDto>.Success(MapDetail(timesheet));
    }

    public async Task<AdminResult<IReadOnlyList<MissingTimesheetReminderDto>>> GetMissingTimesheetRemindersAsync(
        int managerUserId,
        DateOnly? weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var targetWeek = weekStartDate is null
            ? NormalizeWeekStart(DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(-7)
            : NormalizeWeekStart(weekStartDate.Value);

        var team = await employeeRepository.ListByManagerIdAsync(managerUserId, cancellationToken);
        var reminders = new List<MissingTimesheetReminderDto>();

        foreach (var employee in team.Where(member => member.IsActive))
        {
            var exists = await timesheetRepository.ExistsForEmployeeWeekAsync(employee.Id, targetWeek, cancellationToken);
            if (!exists)
            {
                reminders.Add(new MissingTimesheetReminderDto(employee.Id, employee.FullName, employee.Email, targetWeek));
            }
        }

        return AdminResult<IReadOnlyList<MissingTimesheetReminderDto>>.Success(reminders.OrderBy(item => item.EmployeeName).ToList());
    }

    private async Task<Employee?> GetEmployeeByUserIdAsync(int employeeUserId, CancellationToken cancellationToken)
    {
        return await employeeRepository.GetByUserIdAsync(employeeUserId, cancellationToken);
    }

    private async Task<decimal> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken)
    {
        var configuration = await systemConfigurationRepository.GetByKeyAsync("MaxWeeklyHours", cancellationToken);
        if (configuration is not null && decimal.TryParse(configuration.Value, out var configuredHours))
        {
            return configuredHours;
        }

        return BusinessRules.DefaultMaxWeeklyHours;
    }

    private static DateOnly NormalizeWeekStart(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        var daysFromMonday = ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }

    private static bool AllocationCoversWeek(Allocation allocation, DateOnly weekStart)
    {
        if (allocation.Status != AllocationStatus.Active)
        {
            return false;
        }

        var weekEnd = weekStart.AddDays(6);
        var allocationEnd = allocation.ToDate ?? DateOnly.MaxValue;
        return allocation.FromDate <= weekEnd && weekStart <= allocationEnd;
    }

    private static TimesheetSummaryDto MapSummary(Timesheet timesheet)
    {
        return new TimesheetSummaryDto(
            timesheet.Id,
            timesheet.EmployeeId,
            timesheet.Employee.FullName,
            timesheet.WeekStartDate,
            timesheet.TotalHours,
            timesheet.Status,
            timesheet.SubmittedAtUtc);
    }

    private static TimesheetDetailDto MapDetail(Timesheet timesheet)
    {
        var entries = timesheet.Entries
            .Select(entry => new TimesheetEntryDto(
                entry.Id,
                entry.ProjectId,
                entry.Project.Name,
                entry.HoursWorked,
                entry.Notes,
                entry.ActivityTags.Select(tag => tag.Name).OrderBy(name => name).ToList()))
            .ToList();

        return new TimesheetDetailDto(
            timesheet.Id,
            timesheet.EmployeeId,
            timesheet.Employee.FullName,
            timesheet.WeekStartDate,
            timesheet.TotalHours,
            timesheet.Status,
            timesheet.SubmittedAtUtc,
            entries);
    }
}
