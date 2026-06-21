namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record FrozenTimesheetEmployeeDto(
    int UserId,
    string FullName,
    string Email,
    DateOnly? MissingWeekStartDate,
    int ReminderCount,
    bool IsTimesheetSubmissionFrozen,
    DateTime? FrozenAtUtc);
