namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class AdminResult<T>
{
    public bool Succeeded { get; init; }
    public AdminResultCode Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Value { get; init; }

    public static AdminResult<T> Success(T? value, string message = "Success")
    {
        return new AdminResult<T>
        {
            Succeeded = true,
            Code = AdminResultCode.Success,
            Message = message,
            Value = value
        };
    }

    public static AdminResult<T> Fail(AdminResultCode code, string message)
    {
        return new AdminResult<T>
        {
            Succeeded = false,
            Code = code,
            Message = message
        };
    }
}
