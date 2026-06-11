using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class Allocation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public int CreatedByUserId { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public AllocationStatus Status { get; set; } = AllocationStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
