namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record EmployeeTimesheetReminderDto(
    bool HasMissingTimesheet,
    DateOnly? MissingWeekStartDate,
    bool IsTimesheetSubmissionFrozen,
    int ReminderCount,
    string? Message);
