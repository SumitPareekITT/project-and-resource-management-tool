using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data.Repositories;

public sealed class TimesheetRepository(ApplicationDbContext dbContext)
{
    public Task<Timesheet?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Include(timesheet => timesheet.User)
            .ThenInclude(user => user.Profile)
            .Include(timesheet => timesheet.Entries)
            .ThenInclude(entry => entry.Project)
            .Include(timesheet => timesheet.Entries)
            .ThenInclude(entry => entry.ActivityTags)
            .FirstOrDefaultAsync(timesheet => timesheet.Id == id, cancellationToken);
    }

    public Task<Timesheet?> GetByUserWeekAsync(
        int userId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Include(timesheet => timesheet.User)
            .ThenInclude(user => user.Profile)
            .Include(timesheet => timesheet.Entries)
            .ThenInclude(entry => entry.Project)
            .Include(timesheet => timesheet.Entries)
            .ThenInclude(entry => entry.ActivityTags)
            .FirstOrDefaultAsync(
                timesheet => timesheet.UserId == userId && timesheet.WeekStartDate == weekStartDate,
                cancellationToken);
    }

    public Task<List<Timesheet>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Include(timesheet => timesheet.User)
            .ThenInclude(user => user.Profile)
            .Where(timesheet => timesheet.UserId == userId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Timesheet>> ListByManagerTeamAsync(int managerUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets
            .Include(timesheet => timesheet.User)
            .ThenInclude(user => user.Profile)
            .Where(timesheet => timesheet.User.Profile!.ManagerUserId == managerUserId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ThenBy(timesheet => timesheet.User.Profile!.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<decimal> SumProjectHoursForWeekAsync(
        int projectId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.TimesheetEntries
            .Where(entry => entry.ProjectId == projectId && entry.Timesheet.WeekStartDate == weekStartDate)
            .SumAsync(entry => entry.HoursWorked, cancellationToken);
    }

    public async Task<HashSet<int>> ListSubmittedUserIdsForProjectWeekAsync(
        int projectId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var userIds = await dbContext.TimesheetEntries
            .Where(entry => entry.ProjectId == projectId && entry.Timesheet.WeekStartDate == weekStartDate)
            .Select(entry => entry.Timesheet.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return userIds.ToHashSet();
    }

    public Task<bool> ExistsForUserWeekAsync(int userId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        return dbContext.Timesheets.AnyAsync(
            timesheet => timesheet.UserId == userId && timesheet.WeekStartDate == weekStartDate,
            cancellationToken);
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
