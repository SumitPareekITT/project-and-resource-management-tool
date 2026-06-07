using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services.Admin;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<AllocationRepository>();
builder.Services.AddScoped<TimesheetRepository>();
builder.Services.AddScoped<SkillRepository>();
builder.Services.AddScoped<EmployeeAdminService>();
builder.Services.AddScoped<SkillAdminService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
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
