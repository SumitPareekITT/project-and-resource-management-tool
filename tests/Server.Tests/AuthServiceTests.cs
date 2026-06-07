using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
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
        dbContext.Users.Add(new User
        {
            Id = 1,
            FullName = "Admin",
            Email = "admin@test.local",
            Username = "admin",
            PasswordHash = hasher.Hash("Admin@1234"),
            Role = UserRole.Admin,
            IsActive = true,
            ForcePasswordChange = false
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, hasher);
        var result = await service.LoginAsync(new LoginRequest("admin", "Admin@1234"));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Admin, result.Value!.Role);
    }

    [Fact]
    public async Task LoginAsync_Fails_WithInvalidPassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        dbContext.Users.Add(new User
        {
            Id = 1,
            FullName = "Admin",
            Email = "admin@test.local",
            Username = "admin",
            PasswordHash = hasher.Hash("Admin@1234"),
            Role = UserRole.Admin,
            IsActive = true
        });
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
        dbContext.Users.Add(new User
        {
            Id = 2,
            FullName = "Manager",
            Email = "manager@test.local",
            Username = "manager",
            PasswordHash = hasher.Hash("Temp@1234"),
            Role = UserRole.Manager,
            IsActive = true,
            ForcePasswordChange = true
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, hasher);
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest(2, "NewPass@123", "NewPass@123"));

        Assert.True(result.IsSuccess);
        var user = await dbContext.Users.SingleAsync(item => item.Id == 2);
        Assert.False(user.ForcePasswordChange);
        Assert.True(hasher.Verify("NewPass@123", user.PasswordHash));
    }

    private static AuthService CreateService(ApplicationDbContext dbContext, IPasswordHasher hasher)
    {
        return new AuthService(new UserRepository(dbContext), hasher);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
