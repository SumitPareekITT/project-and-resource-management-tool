namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UpdateUserProfileRequest(
    string FullName,
    string Email,
    string Department,
    string Designation,
    int? ManagerUserId);
