using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class UserProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public int? ManagerUserId { get; set; }
    public EmployeeStatus ResourceStatus { get; set; } = EmployeeStatus.Bench;
    public decimal CurrentUtilizationPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTimesheetSubmissionFrozen { get; set; }
    public DateOnly? TimesheetComplianceMissingWeek { get; set; }
    public int TimesheetReminderCount { get; set; }
    public DateOnly? LastTimesheetReminderSentOn { get; set; }
    public DateTime? TimesheetFrozenAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public User? ManagerUser { get; set; }
}
