using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class UserProfileRepository(ApplicationDbContext dbContext)
{
    public Task<UserProfile?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .ThenInclude(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .Include(profile => profile.ManagerUser)
            .ThenInclude(manager => manager!.Profile)
            .Include(profile => profile.User)
            .ThenInclude(user => user.Skills)
            .ThenInclude(skill => skill.Skill)
            .FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken);
    }

    public Task<UserProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .ThenInclude(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .Include(profile => profile.ManagerUser)
            .ThenInclude(manager => manager!.Profile)
            .Include(profile => profile.User)
            .ThenInclude(user => user.Skills)
            .ThenInclude(skill => skill.Skill)
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
    }

    public Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .Include(profile => profile.ManagerUser)
            .ThenInclude(manager => manager!.Profile)
            .FirstOrDefaultAsync(profile => profile.Email == email, cancellationToken);
    }

    public Task<List<UserProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .ThenInclude(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .Include(profile => profile.ManagerUser)
            .ThenInclude(manager => manager!.Profile)
            .Include(profile => profile.User)
            .ThenInclude(user => user.Skills)
            .ThenInclude(skill => skill.Skill)
            .OrderBy(profile => profile.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Where(profile => profile.IsActive)
            .OrderBy(profile => profile.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> ListActiveWithSkillsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .ThenInclude(user => user.Skills)
            .ThenInclude(skill => skill.Skill)
            .Where(profile => profile.IsActive)
            .OrderBy(profile => profile.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> ListActiveEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .ThenInclude(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .Where(profile => profile.IsActive)
            .Where(profile => profile.User.RoleAssignments.Any(assignment =>
                assignment.Role.RoleName == nameof(Shared.Enums.UserRole.Employee)))
            .OrderBy(profile => profile.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> ListByManagerUserIdAsync(int managerUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles
            .Include(profile => profile.User)
            .ThenInclude(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .Include(profile => profile.User)
            .ThenInclude(user => user.Skills)
            .ThenInclude(skill => skill.Skill)
            .Where(profile => profile.ManagerUserId == managerUserId)
            .OrderBy(profile => profile.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await dbContext.UserProfiles.AddAsync(profile, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
