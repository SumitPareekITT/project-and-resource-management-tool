using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class UserAdminServiceTests
{
    [Fact]
    public async Task CreateUserAsync_CreatesManagerWithForcePasswordChange()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        var service = new UserAdminService(new UserRepository(dbContext), hasher);

        var result = await service.CreateUserAsync(new CreateUserRequest(
            "Manager One",
            "manager.one@test.local",
            "manager.one",
            "Temp@1234",
            UserRole.Manager));

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.ForcePasswordChange);
        Assert.Equal(UserRole.Manager, result.Value.Role);
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsInactiveFlag()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        dbContext.Users.Add(new ProjectResourceManagement.Server.Models.User
        {
            Id = 5,
            FullName = "Employee",
            Email = "employee@test.local",
            Username = "employee",
            PasswordHash = hasher.Hash("Temp@1234"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new UserAdminService(new UserRepository(dbContext), hasher);
        var result = await service.DeactivateUserAsync(5);

        Assert.True(result.Succeeded);
        var user = await dbContext.Users.SingleAsync(item => item.Id == 5);
        Assert.False(user.IsActive);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
