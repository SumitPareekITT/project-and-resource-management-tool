using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class SkillRepository(ApplicationDbContext dbContext)
{
    public Task<Skill?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Skills
            .FirstOrDefaultAsync(skill => skill.Id == id, cancellationToken);
    }

    public Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return dbContext.Skills
            .FirstOrDefaultAsync(
                skill => skill.Name.ToLower() == normalized.ToLower(),
                cancellationToken);
    }

    public Task<List<Skill>> ListAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Skills
            .OrderBy(skill => skill.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        await dbContext.Skills.AddAsync(skill, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
