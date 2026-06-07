using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class MilestoneRepository(ApplicationDbContext dbContext)
{
    public Task<Milestone?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Milestones
            .Include(milestone => milestone.Project)
            .FirstOrDefaultAsync(milestone => milestone.Id == id, cancellationToken);
    }

    public Task<List<Milestone>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Milestones
            .Where(milestone => milestone.ProjectId == projectId)
            .OrderBy(milestone => milestone.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        await dbContext.Milestones.AddAsync(milestone, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
