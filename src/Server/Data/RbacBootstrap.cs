using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data;

/// <summary>
/// Ensures RBAC permissions and role mappings from seed data exist in databases
/// created before new permissions (e.g. team match) were added.
/// </summary>
internal static class RbacBootstrap
{
    public static async Task SyncMissingSeedDataAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var addedPermissions = await EnsurePermissionsAsync(dbContext, cancellationToken);
        var addedRolePermissions = await EnsureRolePermissionsAsync(dbContext, cancellationToken);

        if (addedPermissions > 0)
        {
            logger.LogInformation("RBAC bootstrap added {Count} missing permissions.", addedPermissions);
        }

        if (addedRolePermissions > 0)
        {
            logger.LogInformation("RBAC bootstrap added {Count} missing role-permission mappings.", addedRolePermissions);
        }
    }

    private static async Task<int> EnsurePermissionsAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.Permissions
            .Select(permission => permission.PermissionCode)
            .ToListAsync(cancellationToken);
        var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var seedPermission in RbacSeedData.Permissions)
        {
            if (existingCodeSet.Contains(seedPermission.PermissionCode))
            {
                continue;
            }

            var idTaken = await dbContext.Permissions.AnyAsync(
                permission => permission.Id == seedPermission.Id,
                cancellationToken);

            dbContext.Permissions.Add(new Permission
            {
                Id = idTaken ? 0 : seedPermission.Id,
                PermissionCode = seedPermission.PermissionCode,
                Description = seedPermission.Description,
                HttpMethod = seedPermission.HttpMethod,
                RoutePattern = seedPermission.RoutePattern,
                IsActive = seedPermission.IsActive
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    private static async Task<int> EnsureRolePermissionsAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var permissionIdsByCode = await dbContext.Permissions
            .ToDictionaryAsync(
                permission => permission.PermissionCode,
                permission => permission.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        var existingMappings = await dbContext.RolePermissions
            .Select(mapping => new { mapping.RoleId, mapping.PermissionId })
            .ToListAsync(cancellationToken);
        var existingMappingSet = existingMappings
            .Select(mapping => (mapping.RoleId, mapping.PermissionId))
            .ToHashSet();

        var added = 0;
        foreach (var seedPermission in RbacSeedData.Permissions)
        {
            if (!permissionIdsByCode.TryGetValue(seedPermission.PermissionCode, out var permissionId))
            {
                continue;
            }

            var roleIds = RbacSeedData.RolePermissions
                .Where(mapping => mapping.PermissionId == seedPermission.Id)
                .Select(mapping => mapping.RoleId);

            foreach (var roleId in roleIds)
            {
                if (existingMappingSet.Contains((roleId, permissionId)))
                {
                    continue;
                }

                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
                existingMappingSet.Add((roleId, permissionId));
                added++;
            }
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return added;
    }
}
