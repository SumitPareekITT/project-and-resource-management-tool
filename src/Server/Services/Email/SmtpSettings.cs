namespace ProjectResourceManagement.Server.Services.Email;

/// <summary>
/// SMTP settings for outbound email. Configure in appsettings under the "Smtp" section, for example:
/// <code>
/// "Smtp": {
///   "Enabled": true,
///   "Host": "smtp.gmail.com",
///   "Port": 587,
///   "UseSsl": true,
///   "Username": "your-account@gmail.com",
///   "Password": "your-app-password",
///   "FromAddress": "your-account@gmail.com",
///   "FromDisplayName": "PRM Timesheets"
/// }
/// </code>
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromDisplayName { get; init; } = "PRM Timesheets";

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(FromAddress);
}
