using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class Day4ApiIntegrationTests : IClassFixture<Day4ApiFactory>
{
    private readonly HttpClient _client;

    public Day4ApiIntegrationTests(Day4ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!dbContext.Users.Any(user => user.Id == 2))
        {
            var manager = SchemaV3TestHelpers.SeedUser(
                dbContext, 2, "manager.demo", "Manager Demo", "manager.demo@test.local", UserRole.Manager);
            manager.PasswordHash = new Pbkdf2PasswordHasher().Hash("Manager@1234");
            dbContext.SaveChanges();
        }

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SkillsEndpoint_ReturnsUnauthorized_WhenBearerTokenMissing()
    {
        var response = await _client.GetAsync("/api/skills");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SkillsEndpoint_ReturnsForbidden_WhenRoleIsNotAdmin()
    {
        var token = await AuthTestHelper.LoginAndGetTokenAsync(_client, "manager.demo", "Manager@1234");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/skills");
        AuthTestHelper.SetBearerToken(request, token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndListSkill_WorksForAdminRole()
    {
        var token = await AuthTestHelper.LoginAndGetTokenAsync(_client, "admin", "Admin@1234");

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/skills")
        {
            Content = JsonContent.Create(new UpsertSkillRequest("Kubernetes", SkillCategory.DevOps))
        };
        AuthTestHelper.SetBearerToken(createRequest, token);

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/skills");
        AuthTestHelper.SetBearerToken(listRequest, token);

        var listResponse = await _client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var body = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Kubernetes", body);
    }
}

public sealed class Day4ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var databaseName = $"day4_api_{Guid.NewGuid():N}";
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
