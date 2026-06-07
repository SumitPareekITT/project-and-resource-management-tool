using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class Day4ApiIntegrationTests : IClassFixture<Day4ApiFactory>
{
    private readonly HttpClient _client;

    public Day4ApiIntegrationTests(Day4ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SkillsEndpoint_ReturnsUnauthorized_WhenRoleHeaderMissing()
    {
        var response = await _client.GetAsync("/api/skills");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SkillsEndpoint_ReturnsForbidden_WhenRoleIsNotAdmin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/skills");
        request.Headers.Add("X-User-Role", "Manager");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndListSkill_WorksForAdminRole()
    {
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/skills")
        {
            Content = JsonContent.Create(new UpsertSkillRequest("Kubernetes", SkillCategory.DevOps))
        };
        createRequest.Headers.Add("X-User-Role", "Admin");

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/skills");
        listRequest.Headers.Add("X-User-Role", "Admin");

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
