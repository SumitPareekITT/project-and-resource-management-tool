using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Auth;

public sealed record LoginResponse(
    int UserId,
    string FullName,
    string Username,
    UserRole Role,
    bool ForcePasswordChange);
