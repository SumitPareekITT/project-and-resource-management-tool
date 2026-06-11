using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class UserRepository(ApplicationDbContext dbContext)
{
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .Include(user => user.Profile)
            .Include(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .Include(user => user.Profile)
            .Include(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .FirstOrDefaultAsync(user => user.Username == username, cancellationToken);
    }

    public Task<List<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .Include(user => user.Profile)
            .Include(user => user.RoleAssignments)
            .ThenInclude(assignment => assignment.Role)
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
