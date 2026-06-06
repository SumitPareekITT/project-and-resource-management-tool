namespace ProjectResourceManagement.Shared.DTOs.Auth;

public sealed record AuthResult<T>(
    bool IsSuccess,
    AuthResultCode Code,
    string Message,
    T? Value = default)
{
    public static AuthResult<T> Success(T value, string message = "Success")
    {
        return new AuthResult<T>(true, AuthResultCode.Success, message, value);
    }

    public static AuthResult<T> Failure(AuthResultCode code, string message)
    {
        return new AuthResult<T>(false, code, message);
    }
}
