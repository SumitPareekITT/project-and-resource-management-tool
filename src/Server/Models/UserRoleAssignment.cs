namespace ProjectResourceManagement.Server.Models;

public sealed class UserRoleAssignment
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
