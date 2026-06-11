using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class AllocationRepository(ApplicationDbContext dbContext)
{
    public Task<Allocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.User)
            .ThenInclude(user => user.Profile)
            .Include(allocation => allocation.Project)
            .FirstOrDefaultAsync(allocation => allocation.Id == id, cancellationToken);
    }

    public Task<List<Allocation>> ListActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.Project)
            .Where(allocation => allocation.UserId == userId && allocation.Status == AllocationStatus.Active)
            .OrderBy(allocation => allocation.FromDate)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Allocation>> ListActiveByProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.User)
            .ThenInclude(user => user.Profile)
            .Where(allocation => allocation.ProjectId == projectId && allocation.Status == AllocationStatus.Active)
            .OrderBy(allocation => allocation.User.Profile!.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Allocation>> ListAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.User)
            .ThenInclude(user => user.Profile)
            .Include(allocation => allocation.Project)
            .ThenInclude(project => project.ManagerUser)
            .ThenInclude(manager => manager.Profile)
            .Where(allocation => allocation.Status == AllocationStatus.Active)
            .OrderBy(allocation => allocation.Project.Name)
            .ThenBy(allocation => allocation.User.Profile!.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        await dbContext.Allocations.AddAsync(allocation, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
