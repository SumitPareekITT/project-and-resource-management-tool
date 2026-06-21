using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class ProjectAtRiskNotificationLog
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int ManagerUserId { get; set; }
    public ProjectHealthStatus HealthStatus { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
