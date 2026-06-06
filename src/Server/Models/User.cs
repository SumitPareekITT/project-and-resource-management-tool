using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool ForcePasswordChange { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }

    public Employee? EmployeeProfile { get; set; }
    public ICollection<Employee> ManagedEmployees { get; set; } = new List<Employee>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
}
