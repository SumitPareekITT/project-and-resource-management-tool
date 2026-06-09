namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record TimesheetEntryDto(
    int EntryId,
    int ProjectId,
    string ProjectName,
    decimal HoursWorked,
    string Notes,
    IReadOnlyList<string> ActivityTags);
