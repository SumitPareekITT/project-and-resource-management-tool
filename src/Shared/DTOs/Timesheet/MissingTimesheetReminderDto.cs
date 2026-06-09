namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record MissingTimesheetReminderDto(
    int EmployeeId,
    string EmployeeName,
    string Email,
    DateOnly WeekStartDate);
