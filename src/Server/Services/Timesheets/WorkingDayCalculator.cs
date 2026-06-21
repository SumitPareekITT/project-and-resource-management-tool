namespace ProjectResourceManagement.Server.Services.Timesheets;

public static class WorkingDayCalculator
{
    public static bool IsWorkingDay(DateOnly date) =>
        date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    public static DateOnly GetFirstWorkingDayOnOrAfter(DateOnly date)
    {
        var current = date;
        while (!IsWorkingDay(current))
        {
            current = current.AddDays(1);
        }

        return current;
    }

    /// <summary>
    /// First working day after a Mon-Sun week (weekStart is Monday).
    /// </summary>
    public static DateOnly GetFirstWorkingDayAfterWeek(DateOnly weekStartMonday) =>
        GetFirstWorkingDayOnOrAfter(weekStartMonday.AddDays(7));

    public static int CountWorkingDaysInclusive(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return 0;
        }

        var count = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (IsWorkingDay(date))
            {
                count++;
            }
        }

        return count;
    }
}
