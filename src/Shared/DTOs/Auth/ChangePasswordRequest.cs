namespace ProjectResourceManagement.Shared.DTOs.Auth;

public sealed record ChangePasswordRequest(
    int UserId,
    string NewPassword,
    string ConfirmPassword);
