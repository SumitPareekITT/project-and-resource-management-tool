using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Ai;
using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Server.Services.Email;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Scheduling;

public sealed class ProjectAtRiskNotificationService(
    ProjectRepository projectRepository,
    UserProfileRepository userProfileRepository,
    ProjectRiskFactAssembler projectRiskFactAssembler,
    ProjectRiskSummaryService projectRiskSummaryService,
    SkillMatchCandidateFilter skillMatchCandidateFilter,
    ProjectAtRiskNotificationLogRepository notificationLogRepository,
    IEmailSender emailSender,
    ILogger<ProjectAtRiskNotificationService> logger) : IProjectAtRiskNotificationService
{
    public async Task TryNotifyIfNewlyAtRiskAsync(
        Project project,
        ProjectHealthStatus previousStatus,
        ProjectHealthService.ProjectHealthEvaluation evaluation,
        CancellationToken cancellationToken = default)
    {
        if (evaluation.HealthStatus != ProjectHealthStatus.AtRisk || previousStatus == ProjectHealthStatus.AtRisk)
        {
            return;
        }

        var loadedProject = await projectRepository.GetByIdAsync(project.Id, cancellationToken);
        if (loadedProject is null)
        {
            return;
        }

        var managerProfile = loadedProject.ManagerUser?.Profile;
        if (managerProfile is null || string.IsNullOrWhiteSpace(managerProfile.Email))
        {
            logger.LogWarning(
                "Skipping at-risk notification for project {ProjectId} because manager email is missing.",
                loadedProject.Id);
            return;
        }

        var facts = await projectRiskFactAssembler.AssembleFactsAsync(loadedProject, cancellationToken);
        var summary = await projectRiskSummaryService.SummarizeAsync(facts, cancellationToken);
        var suggestedHelp = await BuildSuggestedHelpAsync(evaluation, cancellationToken);
        var message = BuildEmailMessage(loadedProject, managerProfile, facts, evaluation, summary.Text, suggestedHelp);

        await LogAndSendAsync(
            loadedProject.Id,
            loadedProject.ManagerUserId,
            managerProfile.Email,
            message.Subject,
            message.Body,
            cancellationToken);

        await notificationLogRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> BuildSuggestedHelpAsync(
        ProjectHealthService.ProjectHealthEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        var organizationProfiles = await userProfileRepository.ListActiveWithSkillsAsync(cancellationToken);
        var query = InferSkillQuery(evaluation.Signals);
        var candidates = skillMatchCandidateFilter
            .FilterDirectTeam(organizationProfiles, query)
            .Take(5)
            .ToList();

        if (candidates.Count == 0)
        {
            return ["No available employees matched the current risk signals. Review organization capacity or hiring needs."];
        }

        return candidates
            .Select(candidate =>
                $"{candidate.Profile.FullName} ({candidate.Profile.Department}) — {candidate.DeterministicExplanation}")
            .ToList();
    }

    private static string InferSkillQuery(IReadOnlyList<string> signals)
    {
        var combined = string.Join(" ", signals);
        if (combined.Contains("timesheet", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("hours", StringComparison.OrdinalIgnoreCase))
        {
            return "developer engineer delivery";
        }

        if (combined.Contains("allocation", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("staff", StringComparison.OrdinalIgnoreCase))
        {
            return "developer engineer backend frontend available bench";
        }

        if (combined.Contains("milestone", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("story-point", StringComparison.OrdinalIgnoreCase))
        {
            return "developer lead engineer project delivery";
        }

        return "developer engineer";
    }

    private static (string Subject, string Body) BuildEmailMessage(
        Project project,
        UserProfile managerProfile,
        ProjectRiskFacts facts,
        ProjectHealthService.ProjectHealthEvaluation evaluation,
        string aiSummary,
        IReadOnlyList<string> suggestedHelp)
    {
        var subject = $"Project at risk — {project.Name}";
        var milestoneSection = facts.MilestoneLines.Count == 0
            ? "  (none defined)"
            : string.Join(Environment.NewLine, facts.MilestoneLines.Select(line => $"  • {line}"));

        var signalSection = evaluation.Signals.Count == 0
            ? "  (none)"
            : string.Join(Environment.NewLine, evaluation.Signals.Select(signal => $"  • {signal}"));

        var helpSection = string.Join(Environment.NewLine, suggestedHelp.Select(line => $"  • {line}"));

        var body = $"""
            Hi {managerProfile.FullName},

            The Project Health Scheduler flagged one of your projects as AT RISK.

            PROJECT DETAILS
            ---------------
            Name: {project.Name}
            Client: {project.ClientName}
            Manager: {managerProfile.FullName}
            Status: {project.Status}
            Schedule: {project.StartDate:yyyy-MM-dd} to {project.EndDate:yyyy-MM-dd}
            Story points: {project.CompletedStoryPoints}/{project.TotalStoryPoints}

            Key milestones:
            {milestoneSection}

            HEALTH STATUS
            -------------
            {FormatHealthIndicator(evaluation.HealthStatus)}

            Risk signals:
            {signalSection}

            AI RISK SUMMARY
            ---------------
            {aiSummary}

            SUGGESTED HELP
            --------------
            Available employees who may help based on current risk signals:
            {helpSection}

            Review the project in PRM and act on staffing, milestones, or timesheet gaps as needed.
            """;

        return (subject, body);
    }

    private static string FormatHealthIndicator(ProjectHealthStatus status) =>
        status switch
        {
            ProjectHealthStatus.OnTrack => "Green — On Track",
            ProjectHealthStatus.Attention => "Amber — Needs Attention",
            ProjectHealthStatus.AtRisk => "Red — At Risk",
            _ => status.ToString()
        };

    private async Task LogAndSendAsync(
        int projectId,
        int managerUserId,
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        await notificationLogRepository.AddAsync(new ProjectAtRiskNotificationLog
        {
            ProjectId = projectId,
            ManagerUserId = managerUserId,
            HealthStatus = ProjectHealthStatus.AtRisk,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            SentAtUtc = DateTime.UtcNow
        }, cancellationToken);

        logger.LogInformation(
            "Project at-risk notification logged: ProjectId={ProjectId}, To={Email}, Subject={Subject}",
            projectId,
            recipientEmail,
            subject);

        try
        {
            await emailSender.SendAsync(recipientEmail, subject, body, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Failed to send project at-risk email to {Email} for ProjectId={ProjectId}",
                recipientEmail,
                projectId);
        }
    }
}
