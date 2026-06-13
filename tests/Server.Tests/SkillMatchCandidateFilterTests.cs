using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services.Ai.Filtering;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Tests;

public sealed class SkillMatchCandidateFilterTests
{
    [Fact]
    public void FilterDirectTeam_ReturnsOnlyAvailableTeamMembers_WithSkillMatches()
    {
        var filter = new SkillMatchCandidateFilter();
        var team = new List<UserProfile>
        {
            CreateProfile(1, 101, "Backend Dev", 50, EmployeeStatus.PartiallyAllocated, [("Backend API Development", SkillCategory.Backend, ProficiencyLevel.Advanced)]),
            CreateProfile(2, 102, "Fully Allocated", 100, EmployeeStatus.Allocated, [("Backend API Development", SkillCategory.Backend, ProficiencyLevel.Expert)]),
            CreateProfile(3, 103, "Frontend Dev", 0, EmployeeStatus.Bench, [("Frontend Development", SkillCategory.Frontend, ProficiencyLevel.Intermediate)])
        };

        var result = filter.FilterDirectTeam(team, "backend api");

        Assert.Single(result);
        Assert.Equal(101, result[0].Profile.UserId);
        Assert.Contains("Backend API Development", result[0].MatchedSkills[0]);
        Assert.DoesNotContain(result, candidate => candidate.Profile.UserId == 102);
    }

    [Fact]
    public void FilterDirectTeam_ExcludesBenchOnlyMatches_WhenQueryHasSkillKeywords()
    {
        var filter = new SkillMatchCandidateFilter();
        var team = new List<UserProfile>
        {
            CreateProfile(1, 101, "Bench Person", 0, EmployeeStatus.Bench, [], designation: "Business Analyst"),
            CreateProfile(2, 102, "Java Dev", 20, EmployeeStatus.PartiallyAllocated,
                [("Java", SkillCategory.Backend, ProficiencyLevel.Intermediate)])
        };

        var result = filter.FilterDirectTeam(team, "java backend developer intermediate");

        Assert.Single(result);
        Assert.Equal(102, result[0].Profile.UserId);
    }

    [Fact]
    public void Tokenize_RemovesShortTokensAndDuplicates()
    {
        var tokens = SkillMatchQueryTokenizer.Tokenize("backend, API  api  q");
        Assert.Equal(["backend", "api"], tokens);
    }

    private static UserProfile CreateProfile(
        int profileId,
        int userId,
        string name,
        decimal utilization,
        EmployeeStatus resourceStatus,
        IReadOnlyList<(string SkillName, SkillCategory Category, ProficiencyLevel Proficiency)> skills,
        string designation = "Developer")
    {
        var user = new User
        {
            Id = userId,
            Username = $"user{userId}",
            PasswordHash = "hash",
            IsActive = true,
            Skills = skills.Select((skill, index) => new UserSkill
            {
                UserId = userId,
                SkillId = index + 1,
                ProficiencyLevel = skill.Proficiency,
                Skill = new Skill { Id = index + 1, Name = skill.SkillName, Category = skill.Category }
            }).ToList()
        };

        return new UserProfile
        {
            Id = profileId,
            UserId = userId,
            FullName = name,
            Email = $"{userId}@test",
            Department = "Engineering",
            Designation = designation,
            IsActive = true,
            CurrentUtilizationPercent = utilization,
            ResourceStatus = resourceStatus,
            User = user
        };
    }
}
