namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record CreateEmployeeProfileRequest(
    int UserId,
    string FullName,
    string Email,
    string Department,
    string Designation,
    int? ManagerId);
