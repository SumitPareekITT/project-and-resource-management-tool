namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record CreateUserProfileRequest(
    int UserId,
    string FullName,
    string Email,
    string Department,
    string Designation,
    int? ManagerUserId);
