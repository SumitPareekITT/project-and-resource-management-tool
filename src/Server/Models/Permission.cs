namespace ProjectResourceManagement.Server.Models;

public sealed class Permission
{
    public int Id { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? RoutePattern { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
