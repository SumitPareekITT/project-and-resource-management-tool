using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Scheduling;

public interface IProjectAtRiskNotificationService
{
    Task TryNotifyIfNewlyAtRiskAsync(
        Project project,
        ProjectHealthStatus previousStatus,
        ProjectHealthService.ProjectHealthEvaluation evaluation,
        CancellationToken cancellationToken = default);
}

public sealed class NullProjectAtRiskNotificationService : IProjectAtRiskNotificationService
{
    public static NullProjectAtRiskNotificationService Instance { get; } = new();

    public Task TryNotifyIfNewlyAtRiskAsync(
        Project project,
        ProjectHealthStatus previousStatus,
        ProjectHealthService.ProjectHealthEvaluation evaluation,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
