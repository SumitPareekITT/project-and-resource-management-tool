using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class SystemConfigurationRepository(ApplicationDbContext dbContext)
{
    public Task<SystemConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return dbContext.SystemConfigurations
            .FirstOrDefaultAsync(configuration => configuration.Key == key, cancellationToken);
    }
}
