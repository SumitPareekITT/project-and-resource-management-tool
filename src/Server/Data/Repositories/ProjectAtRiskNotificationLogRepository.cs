using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class ProjectAtRiskNotificationLogRepository(ApplicationDbContext dbContext)
{
    public async Task AddAsync(ProjectAtRiskNotificationLog log, CancellationToken cancellationToken = default)
    {
        await dbContext.ProjectAtRiskNotificationLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
