namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record SubmitTimesheetEntryRequest(
    int ProjectId,
    decimal HoursWorked,
    string Notes,
    IReadOnlyList<int> ActivityTagIds);
