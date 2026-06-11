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
        SchemaV3TestHelpers.SeedRoles(dbContext);
        await dbContext.SaveChangesAsync();
        var hasher = new Pbkdf2PasswordHasher();
        var service = CreateService(dbContext, hasher);

        var result = await service.CreateUserAsync(new CreateUserRequest(
            "Manager One",
            "manager.one@test.local",
            "manager.one",
            "Temp@1234",
            UserRole.Manager,
            "Delivery",
            "Manager",
            null));

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.ForcePasswordChange);
        Assert.Contains("Manager", result.Value.Roles);
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsInactiveFlag()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        SchemaV3TestHelpers.SeedUser(dbContext, 5, "employee", "Employee", "employee@test.local", UserRole.Employee);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, hasher);
        var result = await service.DeactivateUserAsync(5);

        Assert.True(result.Succeeded);
        var user = await dbContext.Users.SingleAsync(item => item.Id == 5);
        Assert.False(user.IsActive);
    }

    private static UserAdminService CreateService(ApplicationDbContext dbContext, Pbkdf2PasswordHasher hasher)
    {
        return new UserAdminService(
            new UserRepository(dbContext),
            new UserProfileRepository(dbContext),
            new RbacRepository(dbContext),
            hasher);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
