using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Data;
using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Admin;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class SkillAdminServiceTests
{
    [Fact]
    public async Task CreateSkillAsync_Fails_WhenNameAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Skills.Add(new Skill
        {
            Id = 1,
            Name = "React",
            Category = SkillCategory.Frontend,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new SkillAdminService(new SkillRepository(dbContext));
        var result = await service.CreateSkillAsync(new UpsertSkillRequest("React", SkillCategory.Frontend));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminResultCode.Conflict, result.Code);
    }

    [Fact]
    public async Task DeactivateSkillAsync_SetsInactiveFlag()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Skills.Add(new Skill
        {
            Id = 2,
            Name = "Docker",
            Category = SkillCategory.DevOps,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new SkillAdminService(new SkillRepository(dbContext));
        var result = await service.DeactivateSkillAsync(2);

        Assert.True(result.Succeeded);
        var skill = await dbContext.Skills.SingleAsync(item => item.Id == 2);
        Assert.False(skill.IsActive);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
