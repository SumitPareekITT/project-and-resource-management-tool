using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class TimesheetNotificationLog
{
    public int Id { get; set; }
    public int EmployeeUserId { get; set; }
    public int? ManagerUserId { get; set; }
    public TimesheetNotificationType NotificationType { get; set; }
    public DateOnly MissingWeekStart { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientRole { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
