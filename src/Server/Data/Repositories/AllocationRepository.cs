using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class AllocationRepository(ApplicationDbContext dbContext)
{
    public Task<Allocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.Employee)
            .Include(allocation => allocation.Project)
            .FirstOrDefaultAsync(allocation => allocation.Id == id, cancellationToken);
    }

    public Task<List<Allocation>> ListActiveByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.Project)
            .Where(allocation => allocation.EmployeeId == employeeId && allocation.Status == AllocationStatus.Active)
            .OrderBy(allocation => allocation.FromDate)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Allocation>> ListActiveByProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Allocations
            .Include(allocation => allocation.Employee)
            .Where(allocation => allocation.ProjectId == projectId && allocation.Status == AllocationStatus.Active)
            .OrderBy(allocation => allocation.Employee.FullName)
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
