namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record SubmitTimesheetRequest(
    DateOnly WeekStartDate,
    IReadOnlyList<SubmitTimesheetEntryRequest> Entries);
