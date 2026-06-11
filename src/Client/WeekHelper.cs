namespace ProjectResourceManagement.Client;

/// <summary>
/// Shared date helpers for timesheet week calculations (Monday-based weeks).
/// </summary>
internal static class WeekHelper
{
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }

    public static DateOnly GetDefaultTimesheetWeekStart()
    {
        return GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
