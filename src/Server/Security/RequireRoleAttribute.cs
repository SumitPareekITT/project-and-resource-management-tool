using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireRoleAttribute(params UserRole[] allowedRoles) : Attribute
{
    public IReadOnlyList<UserRole> AllowedRoles { get; } = allowedRoles;
}
