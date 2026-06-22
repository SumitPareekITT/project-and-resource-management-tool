using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class TimesheetNotificationLogRepository(ApplicationDbContext dbContext)
{
    public async Task AddAsync(TimesheetNotificationLog log, CancellationToken cancellationToken = default)
    {
        await dbContext.TimesheetNotificationLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
