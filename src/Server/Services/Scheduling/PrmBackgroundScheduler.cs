using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Server.Services.Timesheets;

namespace ProjectResourceManagement.Server.Services.Scheduling;

public sealed class PrmBackgroundScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<PrmBackgroundScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = await GetIntervalMinutesAsync(stoppingToken);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunJobsAsync(stoppingToken);
        }
    }

    private async Task RunJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var utilizationService = scope.ServiceProvider.GetRequiredService<UtilizationComputationService>();
            var projectHealthService = scope.ServiceProvider.GetRequiredService<ProjectHealthService>();
            var timesheetComplianceService = scope.ServiceProvider.GetRequiredService<TimesheetComplianceService>();

            var employeeCount = await utilizationService.SyncAllActiveProfilesAsync(cancellationToken);
            var projectCount = await projectHealthService.EvaluateAndPersistAllProjectsAsync(cancellationToken);
            var complianceCount = await timesheetComplianceService.ProcessDailyComplianceAsync(cancellationToken);

            logger.LogInformation(
                "Scheduler completed utilization sync for {EmployeeCount} employees, health evaluation for {ProjectCount} projects, and timesheet compliance for {ComplianceCount} employees.",
                employeeCount,
                projectCount,
                complianceCount);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Scheduler job run failed.");
        }
    }

    private async Task<int> GetIntervalMinutesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var configurationRepository = scope.ServiceProvider
            .GetRequiredService<Data.Repositories.SystemConfigurationRepository>();
        var configuration = await configurationRepository.GetByKeyAsync("SchedulerIntervalMinutes", cancellationToken);

        return int.TryParse(configuration?.Value, out var parsed) && parsed > 0
            ? parsed
            : BusinessRules.DefaultSchedulerIntervalMinutes;
    }
}
