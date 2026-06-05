namespace ProjectResourceManagement.Server.Models;

public sealed class TimesheetEntry
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public int ProjectId { get; set; }
    public decimal HoursWorked { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Timesheet Timesheet { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<ActivityTag> ActivityTags { get; set; } = new List<ActivityTag>();
}
