using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
    public ProjectHealthStatus HealthStatus { get; set; } = ProjectHealthStatus.OnTrack;
    public int ManagerUserId { get; set; }
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User ManagerUser { get; set; } = null!;
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();
}
