using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Server.Services.Manager;
using ProjectResourceManagement.Server.Services.Scheduling;
using ProjectResourceManagement.Server.Services.Timesheets;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<AllocationRepository>();
builder.Services.AddScoped<TimesheetRepository>();
builder.Services.AddScoped<SkillRepository>();
builder.Services.AddScoped<MilestoneRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserAdminService>();
builder.Services.AddScoped<EmployeeAdminService>();
builder.Services.AddScoped<SkillAdminService>();
builder.Services.AddScoped<ProjectAdminService>();
builder.Services.AddScoped<AllocationManagerService>();
builder.Services.AddScoped<ActivityTagRepository>();
builder.Services.AddScoped<SystemConfigurationRepository>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<UtilizationComputationService>();
builder.Services.AddScoped<ProjectHealthService>();
builder.Services.AddHostedService<PrmBackgroundScheduler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    dbContext.Database.EnsureCreated();

    var adminUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Username == "admin");
    if (adminUser is not null && adminUser.PasswordHash == "CHANGE_ME_WITH_PASSWORD_HASHER")
    {
        adminUser.PasswordHash = passwordHasher.Hash("Admin@1234");
        await dbContext.SaveChangesAsync();
    }
}

app.UseHttpsRedirection();
app.UseMiddleware<RoleGuardMiddleware>();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Application = "ProjectResourceManagement.Server",
    Status = "Healthy",
    TimestampUtc = DateTime.UtcNow
}));

app.Run();

public partial class Program;
