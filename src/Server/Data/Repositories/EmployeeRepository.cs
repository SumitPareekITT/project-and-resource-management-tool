using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class EmployeeRepository(ApplicationDbContext dbContext)
{
    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .Include(employee => employee.User)
            .Include(employee => employee.Manager)
            .Include(employee => employee.Skills)
            .ThenInclude(employeeSkill => employeeSkill.Skill)
            .FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);
    }

    public Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .Include(employee => employee.User)
            .Include(employee => employee.Manager)
            .Include(employee => employee.Skills)
            .ThenInclude(employeeSkill => employeeSkill.Skill)
            .FirstOrDefaultAsync(employee => employee.UserId == userId, cancellationToken);
    }

    public Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .Include(employee => employee.User)
            .Include(employee => employee.Manager)
            .Include(employee => employee.Skills)
            .ThenInclude(employeeSkill => employeeSkill.Skill)
            .FirstOrDefaultAsync(employee => employee.Email == email, cancellationToken);
    }

    public Task<List<Employee>> ListAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .Include(employee => employee.User)
            .Include(employee => employee.Manager)
            .Include(employee => employee.Skills)
            .ThenInclude(employeeSkill => employeeSkill.Skill)
            .OrderBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Employee>> ListByManagerIdAsync(int managerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .Include(employee => employee.User)
            .Include(employee => employee.Manager)
            .Include(employee => employee.Skills)
            .ThenInclude(employeeSkill => employeeSkill.Skill)
            .Where(employee => employee.ManagerId == managerId)
            .OrderBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
