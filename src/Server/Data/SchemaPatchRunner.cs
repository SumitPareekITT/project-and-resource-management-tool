using Microsoft.EntityFrameworkCore;

namespace ProjectResourceManagement.Server.Data;

/// <summary>
/// Applies additive schema changes to existing databases. EnsureCreated does not alter existing tables,
/// so new columns/tables are added here on startup (dev-friendly, non-destructive).
/// </summary>
internal static class SchemaPatchRunner
{
    private const string UserProfilesTable = "UserProfiles";
    private const string NotificationLogsTable = "TimesheetNotificationLogs";
    private const string ProjectAtRiskNotificationLogsTable = "ProjectAtRiskNotificationLogs";

    private static readonly (string Column, string Definition)[] UserProfileColumnPatches =
    [
        ("IsTimesheetSubmissionFrozen", "TINYINT(1) NOT NULL DEFAULT 0"),
        ("TimesheetComplianceMissingWeek", "DATE NULL"),
        ("TimesheetReminderCount", "INT NOT NULL DEFAULT 0"),
        ("LastTimesheetReminderSentOn", "DATE NULL"),
        ("TimesheetFrozenAtUtc", "DATETIME(6) NULL")
    ];

    public static async Task ApplyAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var tableNames = await ListTableNamesAsync(dbContext, cancellationToken);
        if (!tableNames.Contains(UserProfilesTable))
        {
            return;
        }

        var patchedColumns = 0;
        foreach (var (column, definition) in UserProfileColumnPatches)
        {
            if (await ColumnExistsAsync(dbContext, UserProfilesTable, column, cancellationToken))
            {
                continue;
            }

            await ExecuteAsync(
                dbContext,
                $"ALTER TABLE `{UserProfilesTable}` ADD COLUMN `{column}` {definition}",
                cancellationToken);
            patchedColumns++;
            logger.LogInformation("Schema patch: added {Table}.{Column}", UserProfilesTable, column);
        }

        if (!tableNames.Contains(NotificationLogsTable))
        {
            await ExecuteAsync(
                dbContext,
                """
                CREATE TABLE `TimesheetNotificationLogs` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `EmployeeUserId` INT NOT NULL,
                    `ManagerUserId` INT NULL,
                    `NotificationType` VARCHAR(30) NOT NULL,
                    `MissingWeekStart` DATE NOT NULL,
                    `RecipientEmail` VARCHAR(200) NOT NULL,
                    `RecipientRole` VARCHAR(50) NOT NULL,
                    `Subject` VARCHAR(250) NOT NULL,
                    `Body` VARCHAR(4000) NOT NULL,
                    `SentAtUtc` DATETIME(6) NOT NULL,
                    CONSTRAINT `PK_TimesheetNotificationLogs` PRIMARY KEY (`Id`)
                )
                """,
                cancellationToken);
            logger.LogInformation("Schema patch: created table {Table}.", NotificationLogsTable);
        }

        if (!tableNames.Contains(ProjectAtRiskNotificationLogsTable))
        {
            await ExecuteAsync(
                dbContext,
                """
                CREATE TABLE `ProjectAtRiskNotificationLogs` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `ProjectId` INT NOT NULL,
                    `ManagerUserId` INT NOT NULL,
                    `HealthStatus` VARCHAR(30) NOT NULL,
                    `RecipientEmail` VARCHAR(200) NOT NULL,
                    `Subject` VARCHAR(250) NOT NULL,
                    `Body` VARCHAR(4000) NOT NULL,
                    `SentAtUtc` DATETIME(6) NOT NULL,
                    CONSTRAINT `PK_ProjectAtRiskNotificationLogs` PRIMARY KEY (`Id`)
                )
                """,
                cancellationToken);
            logger.LogInformation("Schema patch: created table {Table}.", ProjectAtRiskNotificationLogsTable);
        }

        if (patchedColumns > 0)
        {
            logger.LogInformation("Schema patch applied {Count} UserProfiles column(s).", patchedColumns);
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        ApplicationDbContext dbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = @tableName
                  AND column_name = @columnName
                """;

            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@tableName";
            tableParameter.Value = tableName;
            command.Parameters.Add(tableParameter);

            var columnParameter = command.CreateParameter();
            columnParameter.ParameterName = "@columnName";
            columnParameter.Value = columnName;
            command.Parameters.Add(columnParameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result) > 0;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task ExecuteAsync(
        ApplicationDbContext dbContext,
        string sql,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task<HashSet<string>> ListTableNamesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                """;

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
