using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class Timesheet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public decimal TotalHours { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Submitted;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
