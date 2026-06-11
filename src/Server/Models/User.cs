namespace ProjectResourceManagement.Server.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool ForcePasswordChange { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = new List<UserRoleAssignment>();
    public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<Allocation> CreatedAllocations { get; set; } = new List<Allocation>();
}
