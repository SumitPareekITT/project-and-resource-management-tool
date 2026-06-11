namespace ProjectResourceManagement.Shared.DTOs.Auth;

public sealed record ChangePasswordRequest(
    string NewPassword,
    string ConfirmPassword);
