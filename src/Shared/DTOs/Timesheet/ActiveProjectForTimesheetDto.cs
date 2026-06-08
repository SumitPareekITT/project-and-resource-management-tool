namespace ProjectResourceManagement.Shared.DTOs.Timesheet;

public sealed record ActiveProjectForTimesheetDto(
    int ProjectId,
    string ProjectName,
    decimal AllocationPercent,
    decimal MaxHoursForWeek);
