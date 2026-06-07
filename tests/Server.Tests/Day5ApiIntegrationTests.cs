using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class Day5ApiIntegrationTests : IClassFixture<Day5ApiFactory>
{
    private readonly HttpClient _client;

    public Day5ApiIntegrationTests(Day5ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!dbContext.Users.Any(user => user.Id == 2))
        {
            dbContext.Users.Add(new User
            {
                Id = 2,
                FullName = "Manager Demo",
                Email = "manager.demo@test.local",
                Username = "manager.demo",
                PasswordHash = "hash",
                Role = UserRole.Manager,
                IsActive = true
            });
            dbContext.SaveChanges();
        }

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsSuccess_ForSeededAdminAfterHashBootstrap()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "Admin@1234"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WorksForAdminRole()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/projects")
        {
            Content = JsonContent.Create(new CreateProjectRequest(
                "Day5 Project",
                "Client",
                "Desc",
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)),
                2,
                80))
        };
        request.Headers.Add("X-User-Role", "Admin");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

public sealed class Day5ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var databaseName = $"day5_api_{Guid.NewGuid():N}";
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });
        });
    }
}
