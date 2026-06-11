namespace ProjectResourceManagement.Server.Models;

public sealed class Role
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<UserRoleAssignment> UserAssignments { get; set; } = new List<UserRoleAssignment>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
