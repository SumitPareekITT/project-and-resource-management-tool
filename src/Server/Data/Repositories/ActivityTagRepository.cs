using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class ActivityTagRepository(ApplicationDbContext dbContext)
{
    public Task<List<ActivityTag>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.ActivityTags
            .Where(tag => tag.IsActive)
            .OrderBy(tag => tag.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ActivityTag>> ListByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        return dbContext.ActivityTags
            .Where(tag => ids.Contains(tag.Id) && tag.IsActive)
            .ToListAsync(cancellationToken);
    }
}
