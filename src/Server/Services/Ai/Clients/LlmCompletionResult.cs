namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public sealed record LlmCompletionResult(bool Succeeded, string Content, string? ErrorMessage = null)
{
    public static LlmCompletionResult Success(string content) => new(true, content);

    public static LlmCompletionResult Failure(string errorMessage) => new(false, string.Empty, errorMessage);
}
