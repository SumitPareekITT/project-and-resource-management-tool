namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record UserSummaryDto(
    int UserId,
    string FullName,
    string Email,
    string Username,
    IReadOnlyList<string> Roles,
    bool ForcePasswordChange,
    bool IsActive);
