namespace ProjectResourceManagement.Server.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ProjectResourceManagement";
    public string Audience { get; init; } = "ProjectResourceManagement.Client";
    public string Secret { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 480;
}
