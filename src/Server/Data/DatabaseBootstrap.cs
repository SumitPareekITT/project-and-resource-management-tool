using Microsoft.EntityFrameworkCore;

namespace ProjectResourceManagement.Server.Data;

/// <summary>
/// Ensures the database matches the current EF model. EnsureCreated does not upgrade existing schemas,
/// so this recreates the database when v3 tables are missing (local dev only).
/// </summary>
internal static class DatabaseBootstrap
{
    private const string AutoRecreateKey = "Database:AutoRecreateOnSchemaMismatch";
    private const string V3ProfileTable = "userprofiles";

    private static readonly string[] LegacyMarkerTables =
    [
        "employees",
        "employeeskills",
        "employee_skills"
    ];

    public static async Task EnsureCurrentSchemaAsync(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var autoRecreate = configuration.GetValue(AutoRecreateKey, environment.IsDevelopment());

        if (!autoRecreate)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            await SchemaPatchRunner.ApplyAsync(dbContext, logger, cancellationToken);
            return;
        }

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            logger.LogInformation("Database not found. Creating schema...");
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        var tableNames = await ListTableNamesAsync(dbContext, cancellationToken);
        if (tableNames.Count == 0)
        {
            logger.LogInformation("Database is empty. Creating schema...");
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        var hasProfileTable = tableNames.Contains(V3ProfileTable);
        var hasLegacySchema = LegacyMarkerTables.Any(tableNames.Contains);

        if (!hasProfileTable || hasLegacySchema)
        {
            logger.LogWarning(
                "Database schema is outdated (missing '{ProfileTable}' or legacy tables present). " +
                "Dropping and recreating database.",
                V3ProfileTable);

            await dbContext.Database.EnsureDeletedAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            logger.LogInformation("Database recreated with current schema.");
            return;
        }

        logger.LogInformation("Database schema is current.");
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await SchemaPatchRunner.ApplyAsync(dbContext, logger, cancellationToken);
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
