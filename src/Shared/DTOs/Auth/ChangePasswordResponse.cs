namespace ProjectResourceManagement.Shared.DTOs.Auth;

public sealed record ChangePasswordResponse(int UserId, bool ForcePasswordChange);
