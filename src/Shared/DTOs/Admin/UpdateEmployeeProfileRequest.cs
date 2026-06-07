namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UpdateEmployeeProfileRequest(
    string FullName,
    string Email,
    string Department,
    string Designation,
    int? ManagerId);
