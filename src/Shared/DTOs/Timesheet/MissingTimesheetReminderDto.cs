namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record MissingTimesheetReminderDto(
    int UserId,
    string UserName,
    string Email,
    DateOnly WeekStartDate);
