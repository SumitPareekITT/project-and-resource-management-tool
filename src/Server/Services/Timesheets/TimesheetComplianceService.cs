using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Timesheet;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Timesheets;

public sealed class TimesheetComplianceService(
    UserProfileRepository userProfileRepository,
    AllocationRepository allocationRepository,
    TimesheetRepository timesheetRepository,
    TimesheetNotificationLogRepository notificationLogRepository,
    ITimesheetNotificationSender notificationSender)
{
    public Task<int> ProcessDailyComplianceAsync(CancellationToken cancellationToken = default)
    {
        return ProcessDailyComplianceForDateAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
    }

    internal async Task<int> ProcessDailyComplianceForDateAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (!WorkingDayCalculator.IsWorkingDay(today))
        {
            return 0;
        }

        var previousWeekStart = NormalizeWeekStart(today).AddDays(-7);
        var firstActionDay = WorkingDayCalculator.GetFirstWorkingDayAfterWeek(previousWeekStart);
        if (today < firstActionDay)
        {
            return 0;
        }

        var workingDayNumber = WorkingDayCalculator.CountWorkingDaysInclusive(firstActionDay, today);
        var profiles = await userProfileRepository.ListActiveEmployeesAsync(cancellationToken);
        var processed = 0;

        foreach (var profile in profiles)
        {
            if (!await RequiresTimesheetForWeekAsync(profile.UserId, previousWeekStart, cancellationToken))
            {
                await ClearComplianceIfResolvedAsync(profile, previousWeekStart, cancellationToken);
                continue;
            }

            var submitted = await timesheetRepository.ExistsForUserWeekAsync(profile.UserId, previousWeekStart, cancellationToken);
            if (submitted)
            {
                await ClearComplianceIfResolvedAsync(profile, previousWeekStart, cancellationToken);
                continue;
            }

            await ProcessMissingTimesheetAsync(profile, previousWeekStart, workingDayNumber, today, cancellationToken);
            processed++;
        }

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        await notificationLogRepository.SaveChangesAsync(cancellationToken);
        return processed;
    }

    public async Task<AdminResult<IReadOnlyList<FrozenTimesheetEmployeeDto>>> ListFrozenTeamMembersAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await userProfileRepository.ListByManagerUserIdAsync(managerUserId, cancellationToken);
        var frozen = team
            .Where(profile => profile.IsActive && profile.IsTimesheetSubmissionFrozen)
            .Select(MapFrozenEmployee)
            .OrderBy(item => item.FullName)
            .ToList();

        return AdminResult<IReadOnlyList<FrozenTimesheetEmployeeDto>>.Success(frozen);
    }

    public async Task<AdminResult<FrozenTimesheetEmployeeDto>> RestoreTimesheetAccessAsync(
        int managerUserId,
        int employeeUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(employeeUserId, cancellationToken);
        if (profile is null || !profile.IsActive)
        {
            return AdminResult<FrozenTimesheetEmployeeDto>.Fail(AdminResultCode.NotFound, "Employee profile was not found.");
        }

        if (profile.ManagerUserId != managerUserId)
        {
            return AdminResult<FrozenTimesheetEmployeeDto>.Fail(
                AdminResultCode.ValidationError,
                "You can restore timesheet access only for your direct reports.");
        }

        if (!profile.IsTimesheetSubmissionFrozen)
        {
            return AdminResult<FrozenTimesheetEmployeeDto>.Fail(
                AdminResultCode.ValidationError,
                "Timesheet submission is not frozen for this employee.");
        }

        profile.IsTimesheetSubmissionFrozen = false;
        profile.TimesheetFrozenAtUtc = null;
        profile.TimesheetReminderCount = 0;
        profile.LastTimesheetReminderSentOn = null;

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<FrozenTimesheetEmployeeDto>.Success(MapFrozenEmployee(profile));
    }

    internal static DateOnly NormalizeWeekStart(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }

    private async Task ProcessMissingTimesheetAsync(
        UserProfile profile,
        DateOnly missingWeekStart,
        int workingDayNumber,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        profile.TimesheetComplianceMissingWeek = missingWeekStart;

        if (workingDayNumber == 1 && profile.TimesheetReminderCount < 1 && profile.LastTimesheetReminderSentOn != today)
        {
            await SendReminderAsync(profile, TimesheetNotificationType.Reminder1, missingWeekStart, today, cancellationToken);
            profile.TimesheetReminderCount = 1;
            return;
        }

        if (workingDayNumber == 2 && profile.TimesheetReminderCount < 2 && profile.LastTimesheetReminderSentOn != today)
        {
            await SendReminderAsync(profile, TimesheetNotificationType.Reminder2, missingWeekStart, today, cancellationToken);
            profile.TimesheetReminderCount = 2;
            return;
        }

        if (workingDayNumber >= 3 && profile.TimesheetReminderCount >= 2 && !profile.IsTimesheetSubmissionFrozen)
        {
            profile.IsTimesheetSubmissionFrozen = true;
            profile.TimesheetFrozenAtUtc = DateTime.UtcNow;
            await SendReminderAsync(profile, TimesheetNotificationType.AccountFrozen, missingWeekStart, today, cancellationToken);
        }
    }

    private async Task SendReminderAsync(
        UserProfile profile,
        TimesheetNotificationType notificationType,
        DateOnly missingWeekStart,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        UserProfile? managerProfile = null;
        if (profile.ManagerUserId is int managerUserId)
        {
            managerProfile = await userProfileRepository.GetByUserIdAsync(managerUserId, cancellationToken);
        }

        await notificationSender.SendAsync(notificationType, profile, managerProfile, missingWeekStart, cancellationToken);
        profile.LastTimesheetReminderSentOn = today;
    }

    private async Task ClearComplianceIfResolvedAsync(
        UserProfile profile,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        if (profile.TimesheetComplianceMissingWeek == weekStart
            || await timesheetRepository.ExistsForUserWeekAsync(profile.UserId, weekStart, cancellationToken))
        {
            profile.TimesheetComplianceMissingWeek = null;
            profile.TimesheetReminderCount = 0;
            profile.LastTimesheetReminderSentOn = null;
            profile.IsTimesheetSubmissionFrozen = false;
            profile.TimesheetFrozenAtUtc = null;
        }
    }

    private async Task<bool> RequiresTimesheetForWeekAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var allocations = await allocationRepository.ListActiveByUserIdAsync(userId, cancellationToken);
        return allocations.Any(allocation => AllocationCoversWeek(allocation, weekStart));
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

    private static FrozenTimesheetEmployeeDto MapFrozenEmployee(UserProfile profile) =>
        new(
            profile.UserId,
            profile.FullName,
            profile.Email,
            profile.TimesheetComplianceMissingWeek,
            profile.TimesheetReminderCount,
            profile.IsTimesheetSubmissionFrozen,
            profile.TimesheetFrozenAtUtc);
}
