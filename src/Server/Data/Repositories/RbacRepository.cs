using Microsoft.EntityFrameworkCore;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class RbacRepository(ApplicationDbContext dbContext)
{
    public async Task<HashSet<string>> GetPermissionCodesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var codes = await dbContext.UserRoleAssignments
            .Where(assignment => assignment.UserId == userId)
            .SelectMany(assignment => assignment.Role.RolePermissions)
            .Where(rolePermission => rolePermission.Permission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task<List<string>> GetRoleNamesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserRoleAssignments
            .Where(assignment => assignment.UserId == userId && assignment.Role.IsActive)
            .Select(assignment => assignment.Role.RoleName)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<Models.Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles.FirstOrDefaultAsync(role => role.RoleName == roleName, cancellationToken);
    }

    public async Task AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.UserRoleAssignments
            .AnyAsync(assignment => assignment.UserId == userId && assignment.RoleId == roleId, cancellationToken);
        if (!exists)
        {
            await dbContext.UserRoleAssignments.AddAsync(new Models.UserRoleAssignment
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
