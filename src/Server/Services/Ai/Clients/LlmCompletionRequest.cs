namespace ProjectResourceManagement.Server.Services.Ai.Clients;

public sealed record LlmCompletionRequest(string SystemInstruction, string UserPrompt);
