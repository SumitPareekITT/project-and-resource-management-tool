using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class ProjectRepository(ApplicationDbContext dbContext)
{
    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .Include(project => project.Manager)
            .Include(project => project.Milestones)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public Task<List<Project>> ListAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .Include(project => project.Manager)
            .Include(project => project.Milestones)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Project>> ListByManagerIdAsync(int managerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .Where(project => project.ManagerId == managerId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
