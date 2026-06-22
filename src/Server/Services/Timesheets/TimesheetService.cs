using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Timesheet;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Timesheets;

public sealed class TimesheetService(
    UserProfileRepository userProfileRepository,
    AllocationRepository allocationRepository,
    TimesheetRepository timesheetRepository,
    ActivityTagRepository activityTagRepository,
    SystemConfigurationRepository systemConfigurationRepository)
{
    public async Task<AdminResult<IReadOnlyList<ActiveProjectForTimesheetDto>>> GetActiveProjectsForWeekAsync(
        int userId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<IReadOnlyList<ActiveProjectForTimesheetDto>>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var normalizedWeek = NormalizeWeekStart(weekStartDate);
        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var allocations = await allocationRepository.ListActiveByUserIdAsync(userId, cancellationToken);
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
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Entries.Count == 0)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.ValidationError, "At least one timesheet entry is required.");
        }

        var profile = await GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile is null || !profile.IsActive)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "User profile was not found or is inactive.");
        }

        if (profile.IsTimesheetSubmissionFrozen)
        {
            return AdminResult<TimesheetDetailDto>.Fail(
                AdminResultCode.ValidationError,
                "Timesheet submission is frozen due to missed submissions after reminders. Contact your manager to restore access.");
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

        if (await timesheetRepository.ExistsForUserWeekAsync(userId, weekStart, cancellationToken))
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

        var allocations = await allocationRepository.ListActiveByUserIdAsync(userId, cancellationToken);
        var weekAllocations = allocations.Where(allocation => AllocationCoversWeek(allocation, weekStart)).ToList();

        var timesheet = new Timesheet
        {
            UserId = userId,
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

        if (profile.TimesheetComplianceMissingWeek == weekStart || profile.IsTimesheetSubmissionFrozen)
        {
            profile.IsTimesheetSubmissionFrozen = false;
            profile.TimesheetFrozenAtUtc = null;
            profile.TimesheetComplianceMissingWeek = null;
            profile.TimesheetReminderCount = 0;
            profile.LastTimesheetReminderSentOn = null;
            await userProfileRepository.SaveChangesAsync(cancellationToken);
        }

        await timesheetRepository.SaveChangesAsync(cancellationToken);

        var created = await timesheetRepository.GetByIdAsync(timesheet.Id, cancellationToken);
        return AdminResult<TimesheetDetailDto>.Success(MapDetail(created!));
    }

    public async Task<AdminResult<IReadOnlyList<TimesheetSummaryDto>>> ListEmployeeHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (await GetProfileByUserIdAsync(userId, cancellationToken) is null)
        {
            return AdminResult<IReadOnlyList<TimesheetSummaryDto>>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var timesheets = await timesheetRepository.ListByUserIdAsync(userId, cancellationToken);
        return AdminResult<IReadOnlyList<TimesheetSummaryDto>>.Success(timesheets.Select(MapSummary).ToList());
    }

    public async Task<AdminResult<TimesheetDetailDto>> GetEmployeeTimesheetAsync(
        int userId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        if (await GetProfileByUserIdAsync(userId, cancellationToken) is null)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var timesheet = await timesheetRepository.GetByUserWeekAsync(userId, NormalizeWeekStart(weekStartDate), cancellationToken);
        if (timesheet is null)
        {
            return AdminResult<TimesheetDetailDto>.Fail(AdminResultCode.NotFound, "Timesheet was not found.");
        }

        return AdminResult<TimesheetDetailDto>.Success(MapDetail(timesheet));
    }

    public async Task<AdminResult<IReadOnlyList<EmployeeAllocationDto>>> ListEmployeeAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (await GetProfileByUserIdAsync(userId, cancellationToken) is null)
        {
            return AdminResult<IReadOnlyList<EmployeeAllocationDto>>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var allocations = await allocationRepository.ListActiveByUserIdAsync(userId, cancellationToken);
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

        if (timesheet.User.Profile?.ManagerUserId != managerUserId)
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

        var team = await userProfileRepository.ListByManagerUserIdAsync(managerUserId, cancellationToken);
        var reminders = new List<MissingTimesheetReminderDto>();

        foreach (var profile in team.Where(member => member.IsActive))
        {
            var exists = await timesheetRepository.ExistsForUserWeekAsync(profile.UserId, targetWeek, cancellationToken);
            if (!exists)
            {
                reminders.Add(new MissingTimesheetReminderDto(
                    profile.UserId,
                    profile.FullName,
                    profile.Email,
                    targetWeek,
                    profile.TimesheetComplianceMissingWeek == targetWeek ? profile.TimesheetReminderCount : 0,
                    profile.IsTimesheetSubmissionFrozen && profile.TimesheetComplianceMissingWeek == targetWeek));
            }
        }

        return AdminResult<IReadOnlyList<MissingTimesheetReminderDto>>.Success(reminders.OrderBy(item => item.UserName).ToList());
    }

    public async Task<AdminResult<IReadOnlyList<ActivityTagOptionDto>>> ListActivityTagsAsync(
        CancellationToken cancellationToken = default)
    {
        var tags = await activityTagRepository.ListActiveAsync(cancellationToken);
        var mapped = tags
            .Select(tag => new ActivityTagOptionDto(tag.Id, tag.Name, tag.Category))
            .ToList();

        return AdminResult<IReadOnlyList<ActivityTagOptionDto>>.Success(mapped);
    }

    public async Task<AdminResult<EmployeeTimesheetReminderDto>> GetEmployeeMissingTimesheetReminderAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (await GetProfileByUserIdAsync(userId, cancellationToken) is not { } profile)
        {
            return AdminResult<EmployeeTimesheetReminderDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var previousWeek = NormalizeWeekStart(DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(-7);
        var exists = await timesheetRepository.ExistsForUserWeekAsync(userId, previousWeek, cancellationToken);

        string? message = null;
        if (profile.IsTimesheetSubmissionFrozen)
        {
            message = profile.TimesheetComplianceMissingWeek is null
                ? "Timesheet submission is frozen. Contact your manager to restore access."
                : $"Timesheet submission is frozen for week {profile.TimesheetComplianceMissingWeek:yyyy-MM-dd}. Contact your manager to restore access.";
        }
        else if (!exists)
        {
            message = profile.TimesheetReminderCount switch
            {
                >= 2 => $"Final reminder: submit timesheet for week {previousWeek:yyyy-MM-dd} today to avoid access freeze.",
                1 => $"Reminder: submit timesheet for week {previousWeek:yyyy-MM-dd}.",
                _ => $"Reminder: Timesheet for week {previousWeek:yyyy-MM-dd} has not been submitted."
            };
        }

        return AdminResult<EmployeeTimesheetReminderDto>.Success(
            new EmployeeTimesheetReminderDto(
                !exists,
                exists ? null : previousWeek,
                profile.IsTimesheetSubmissionFrozen,
                profile.TimesheetComplianceMissingWeek == previousWeek ? profile.TimesheetReminderCount : 0,
                message));
    }

    private Task<UserProfile?> GetProfileByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
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
        var userName = timesheet.User.Profile?.FullName ?? timesheet.User.Username;
        return new TimesheetSummaryDto(
            timesheet.Id,
            timesheet.UserId,
            userName,
            timesheet.WeekStartDate,
            timesheet.TotalHours,
            timesheet.Status,
            timesheet.SubmittedAtUtc);
    }

    private static TimesheetDetailDto MapDetail(Timesheet timesheet)
    {
        var userName = timesheet.User.Profile?.FullName ?? timesheet.User.Username;
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
            timesheet.UserId,
            userName,
            timesheet.WeekStartDate,
            timesheet.TotalHours,
            timesheet.Status,
            timesheet.SubmittedAtUtc,
            entries);
    }
}