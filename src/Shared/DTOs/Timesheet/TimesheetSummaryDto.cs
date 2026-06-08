using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record TimesheetSummaryDto(
    int TimesheetId,
    int EmployeeId,
    string EmployeeName,
    DateOnly WeekStartDate,
    decimal TotalHours,
    TimesheetStatus Status,
    DateTime? SubmittedAtUtc);
