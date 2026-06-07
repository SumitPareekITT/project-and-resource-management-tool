using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UserSummaryDto(
    int UserId,
    string FullName,
    string Email,
    string Username,
    UserRole Role,
    bool ForcePasswordChange,
    bool IsActive);
