namespace ProjectResourceManagement.Shared.DTOs.Auth;

public enum AuthResultCode
{
    Success = 1,
    InvalidCredentials = 2,
    InactiveUser = 3,
    UserNotFound = 4,
    PasswordTooShort = 5,
    PasswordMismatch = 6
}
