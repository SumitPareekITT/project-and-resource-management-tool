namespace ProjectResourceManagement.Server.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute(string permissionCode) : Attribute
{
    public string PermissionCode { get; } = permissionCode;
}
