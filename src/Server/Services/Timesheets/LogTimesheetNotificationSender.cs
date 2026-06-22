using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Email;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Timesheets;

public interface ITimesheetNotificationSender
{
    Task SendAsync(
        TimesheetNotificationType notificationType,
        UserProfile employeeProfile,
        UserProfile? managerProfile,
        DateOnly missingWeekStart,
        CancellationToken cancellationToken = default);
}

public sealed class LogTimesheetNotificationSender(
    TimesheetNotificationLogRepository notificationLogRepository,
    IEmailSender emailSender,
    ILogger<LogTimesheetNotificationSender> logger) : ITimesheetNotificationSender
{
    public async Task SendAsync(
        TimesheetNotificationType notificationType,
        UserProfile employeeProfile,
        UserProfile? managerProfile,
        DateOnly missingWeekStart,
        CancellationToken cancellationToken = default)
    {
        var employeeMessage = BuildEmployeeMessage(notificationType, employeeProfile.FullName, missingWeekStart);
        await LogAsync(
            notificationType,
            employeeProfile.UserId,
            managerProfile?.UserId,
            missingWeekStart,
            employeeProfile.Email,
            "Employee",
            employeeMessage.Subject,
            employeeMessage.Body,
            cancellationToken);

        if (managerProfile is not null && !string.IsNullOrWhiteSpace(managerProfile.Email))
        {
            var managerMessage = BuildManagerMessage(notificationType, employeeProfile.FullName, missingWeekStart);
            await LogAsync(
                notificationType,
                employeeProfile.UserId,
                managerProfile.UserId,
                missingWeekStart,
                managerProfile.Email,
                "Manager",
                managerMessage.Subject,
                managerMessage.Body,
                cancellationToken);
        }
    }

    private async Task LogAsync(
        TimesheetNotificationType notificationType,
        int employeeUserId,
        int? managerUserId,
        DateOnly missingWeekStart,
        string recipientEmail,
        string recipientRole,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        await notificationLogRepository.AddAsync(new TimesheetNotificationLog
        {
            EmployeeUserId = employeeUserId,
            ManagerUserId = managerUserId,
            NotificationType = notificationType,
            MissingWeekStart = missingWeekStart,
            RecipientEmail = recipientEmail,
            RecipientRole = recipientRole,
            Subject = subject,
            Body = body,
            SentAtUtc = DateTime.UtcNow
        }, cancellationToken);

        logger.LogInformation(
            "Timesheet notification logged: Type={Type}, EmployeeUserId={EmployeeUserId}, To={Email}, Subject={Subject}",
            notificationType,
            employeeUserId,
            recipientEmail,
            subject);

        try
        {
            await emailSender.SendAsync(recipientEmail, subject, body, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Failed to send timesheet notification email to {Email}, Type={Type}, EmployeeUserId={EmployeeUserId}",
                recipientEmail,
                notificationType,
                employeeUserId);
        }
    }

    private static (string Subject, string Body) BuildEmployeeMessage(
        TimesheetNotificationType notificationType,
        string employeeName,
        DateOnly missingWeekStart)
    {
        return notificationType switch
        {
            TimesheetNotificationType.Reminder1 => (
                $"Timesheet reminder — week of {missingWeekStart:yyyy-MM-dd}",
                $"Hi {employeeName}, your timesheet for week starting {missingWeekStart:yyyy-MM-dd} is missing. Please submit it today."),
            TimesheetNotificationType.Reminder2 => (
                $"Final timesheet reminder — week of {missingWeekStart:yyyy-MM-dd}",
                $"Hi {employeeName}, this is your second reminder. Submit the timesheet for week starting {missingWeekStart:yyyy-MM-dd} immediately to avoid access restrictions."),
            TimesheetNotificationType.AccountFrozen => (
                $"Timesheet access restricted — week of {missingWeekStart:yyyy-MM-dd}",
                $"Hi {employeeName}, timesheet submission is now frozen because the week starting {missingWeekStart:yyyy-MM-dd} was not submitted after two reminders. You can still log in and view history. Contact your manager to restore access."),
            _ => ("Timesheet notification", $"Timesheet update for week starting {missingWeekStart:yyyy-MM-dd}.")
        };
    }

    private static (string Subject, string Body) BuildManagerMessage(
        TimesheetNotificationType notificationType,
        string employeeName,
        DateOnly missingWeekStart)
    {
        return notificationType switch
        {
            TimesheetNotificationType.Reminder1 => (
                $"Team timesheet reminder — {employeeName}",
                $"{employeeName} has not submitted the timesheet for week starting {missingWeekStart:yyyy-MM-dd}. First reminder sent."),
            TimesheetNotificationType.Reminder2 => (
                $"Team timesheet final reminder — {employeeName}",
                $"{employeeName} still has not submitted the timesheet for week starting {missingWeekStart:yyyy-MM-dd}. Second reminder sent."),
            TimesheetNotificationType.AccountFrozen => (
                $"Team timesheet access frozen — {employeeName}",
                $"{employeeName}'s timesheet submission access is frozen for missing week starting {missingWeekStart:yyyy-MM-dd}. Review and restore access from the Manager menu when appropriate."),
            _ => ("Team timesheet notification", $"{employeeName} timesheet update for week starting {missingWeekStart:yyyy-MM-dd}.")
        };
    }
}
