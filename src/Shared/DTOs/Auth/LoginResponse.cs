namespace ProjectResourceManagement.Shared.DTOs.Auth;

public sealed record LoginResponse(
    int UserId,
    string FullName,
    string Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool ForcePasswordChange,
    string AccessToken,
    DateTime ExpiresAtUtc);
