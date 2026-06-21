using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Manager;
using ProjectResourceManagement.Server.Services.Ai;
using ProjectResourceManagement.Server.Services.Ai.Clients;
using ProjectResourceManagement.Server.Services.Ai.Configuration;
using ProjectResourceManagement.Server.Services.Ai.Facts;
using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Server.Services.Ai.Fallback;
using ProjectResourceManagement.Server.Services.Ai.Prompts;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Server.Services.Timesheets;
using ProjectResourceManagement.Server.Swagger;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
{
    throw new InvalidOperationException("JWT secret must be at least 32 characters.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserProfileRepository>();
builder.Services.AddScoped<RbacRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<AllocationRepository>();
builder.Services.AddScoped<TimesheetRepository>();
builder.Services.AddScoped<SkillRepository>();
builder.Services.AddScoped<MilestoneRepository>();
builder.Services.AddScoped<ActivityTagRepository>();
builder.Services.AddScoped<SystemConfigurationRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserAdminService>();
builder.Services.AddScoped<UserProfileAdminService>();
builder.Services.AddScoped<SkillAdminService>();
builder.Services.AddScoped<ProjectAdminService>();
builder.Services.AddScoped<AllocationManagerService>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<UtilizationComputationService>();
builder.Services.AddScoped<ProjectHealthService>();
builder.Services.AddHostedService<PrmBackgroundScheduler>();
builder.Services.AddHttpClient(nameof(GeminiLlmCompletionClient));
builder.Services.AddHttpClient(nameof(GroqLlmCompletionClient));
builder.Services.AddHttpClient(nameof(GemmaLlmCompletionClient), client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddScoped<ILlmCompletionClient, GeminiLlmCompletionClient>();
builder.Services.AddScoped<ILlmCompletionClient, GroqLlmCompletionClient>();
builder.Services.AddScoped<ILlmCompletionClient, GemmaLlmCompletionClient>();
builder.Services.AddScoped<LlmCompletionClientFactory>();
builder.Services.AddScoped<LlmConfigurationReader>();
builder.Services.AddScoped<SkillMatchCandidateFilter>();
builder.Services.AddScoped<OrganizationTeamMatcher>();
builder.Services.AddScoped<ProjectRiskFactAssembler>();
builder.Services.AddScoped<SkillMatchPromptBuilder>();
builder.Services.AddScoped<ProjectRiskPromptBuilder>();
builder.Services.AddScoped<TeamMatchPromptBuilder>();
builder.Services.AddScoped<DeterministicSkillMatchSummarizer>();
builder.Services.AddScoped<DeterministicProjectRiskSummarizer>();
builder.Services.AddScoped<DeterministicTeamMatchSummarizer>();
builder.Services.AddScoped<AiAssistantService>();
builder.Services.AddScoped<SystemConfigurationAdminService>();
builder.Services.AddPrmSwagger();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseBootstrap");

    await DatabaseBootstrap.EnsureCurrentSchemaAsync(dbContext, configuration, environment, logger);
    await RbacBootstrap.SyncMissingSeedDataAsync(dbContext, logger);

    var adminUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Username == "admin");
    if (adminUser is not null && adminUser.PasswordHash == "CHANGE_ME_WITH_PASSWORD_HASHER")
    {
        adminUser.PasswordHash = passwordHasher.Hash("Admin@1234");
        await dbContext.SaveChangesAsync();
    }

    if (!await dbContext.Skills.AnyAsync())
    {
        await dbContext.Skills.AddRangeAsync(SeedData.Skills);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} skills into empty skills catalog.", SeedData.Skills.Count);
    }
}

app.UsePrmSwaggerUi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PermissionGuardMiddleware>();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Application = "ProjectResourceManagement.Server",
    Status = "Healthy",
    TimestampUtc = DateTime.UtcNow
}));

app.Run();

public partial class Program;
