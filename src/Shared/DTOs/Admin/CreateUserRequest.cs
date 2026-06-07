using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    UserRole Role);
