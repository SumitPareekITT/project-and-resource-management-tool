using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Shared.DTOs.Auth;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_Succeeds_WithValidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        var user = SchemaV3TestHelpers.SeedUser(dbContext, 1, "admin", "Admin", "admin@test.local", UserRole.Admin);
        user.PasswordHash = hasher.Hash("Admin@1234");
        user.ForcePasswordChange = false;
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, hasher);
        var result = await service.LoginAsync(new LoginRequest("admin", "Admin@1234"));

        Assert.True(result.IsSuccess);
        Assert.Contains("Admin", result.Value!.Roles);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
    }

    [Fact]
    public async Task LoginAsync_Fails_WithInvalidPassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        var user = SchemaV3TestHelpers.SeedUser(dbContext, 1, "admin", "Admin", "admin@test.local", UserRole.Admin);
        user.PasswordHash = hasher.Hash("Admin@1234");
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, hasher);
        var result = await service.LoginAsync(new LoginRequest("admin", "wrong"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.InvalidCredentials, result.Code);
    }

    [Fact]
    public async Task ChangePasswordAsync_ClearsForcePasswordChange()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        var user = SchemaV3TestHelpers.SeedUser(dbContext, 2, "manager", "Manager", "manager@test.local", UserRole.Manager);
        user.PasswordHash = hasher.Hash("Temp@1234");
        user.ForcePasswordChange = true;
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, hasher);
        var result = await service.ChangePasswordAsync(2, new ChangePasswordRequest("NewPass@123", "NewPass@123"));

        Assert.True(result.IsSuccess);
        var updated = await dbContext.Users.SingleAsync(item => item.Id == 2);
        Assert.False(updated.ForcePasswordChange);
        Assert.True(hasher.Verify("NewPass@123", updated.PasswordHash));
    }

    private static AuthService CreateService(ApplicationDbContext dbContext, IPasswordHasher hasher)
    {
        var jwtOptions = Options.Create(new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Secret = "prm-test-jwt-secret-for-unit-tests",
            ExpirationMinutes = 60
        });

        return new AuthService(
            new UserRepository(dbContext),
            new RbacRepository(dbContext),
            hasher,
            new JwtTokenService(jwtOptions));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
