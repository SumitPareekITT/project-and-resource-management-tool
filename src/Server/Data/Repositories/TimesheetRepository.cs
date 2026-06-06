using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class TimesheetRepository(ApplicationDbContext dbContext)
{
    public Task<Timesheet?> GetByEmployeeWeekAsync(
        int employeeId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Include(timesheet => timesheet.Entries)
            .ThenInclude(entry => entry.ActivityTags)
            .FirstOrDefaultAsync(
                timesheet => timesheet.EmployeeId == employeeId && timesheet.WeekStartDate == weekStartDate,
                cancellationToken);
    }

    public Task<List<Timesheet>> ListByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Where(timesheet => timesheet.EmployeeId == employeeId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Timesheet>> ListByManagerTeamAsync(int managerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Include(timesheet => timesheet.Employee)
            .Where(timesheet => timesheet.Employee.ManagerId == managerId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ThenBy(timesheet => timesheet.Employee.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Timesheet timesheet, CancellationToken cancellationToken = default)
    {
        await dbContext.Timesheets.AddAsync(timesheet, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
