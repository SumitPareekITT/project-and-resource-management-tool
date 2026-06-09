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
        var team = new List<Employee>
        {
            CreateEmployee(
                id: 1,
                name: "Backend Dev",
                utilization: 50,
                status: EmployeeStatus.PartiallyAllocated,
                skills: [("Backend API Development", SkillCategory.Backend, ProficiencyLevel.Advanced)]),
            CreateEmployee(
                id: 2,
                name: "Fully Allocated",
                utilization: 100,
                status: EmployeeStatus.Allocated,
                skills: [("Backend API Development", SkillCategory.Backend, ProficiencyLevel.Expert)]),
            CreateEmployee(
                id: 3,
                name: "Frontend Dev",
                utilization: 0,
                status: EmployeeStatus.Bench,
                skills: [("Frontend Development", SkillCategory.Frontend, ProficiencyLevel.Intermediate)])
        };

        var result = filter.FilterDirectTeam(team, "backend api");

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Employee.Id);
        Assert.Contains("Backend API Development", result[0].MatchedSkills[0]);
        Assert.DoesNotContain(result, candidate => candidate.Employee.Id == 2);
    }

    [Fact]
    public void Tokenize_RemovesShortTokensAndDuplicates()
    {
        var tokens = SkillMatchQueryTokenizer.Tokenize("backend, API  api  q");

        Assert.Equal(["backend", "api"], tokens);
    }

    private static Employee CreateEmployee(
        int id,
        string name,
        decimal utilization,
        EmployeeStatus status,
        IReadOnlyList<(string SkillName, SkillCategory Category, ProficiencyLevel Proficiency)> skills)
    {
        return new Employee
        {
            Id = id,
            UserId = id + 100,
            FullName = name,
            Email = $"{id}@test",
            Department = "Engineering",
            Designation = "Developer",
            IsActive = true,
            CurrentUtilizationPercent = utilization,
            Status = status,
            Skills = skills.Select((skill, index) => new EmployeeSkill
            {
                EmployeeId = id,
                SkillId = index + 1,
                ProficiencyLevel = skill.Proficiency,
                Skill = new Skill
                {
                    Id = index + 1,
                    Name = skill.SkillName,
                    Category = skill.Category
                }
            }).ToList()
        };
    }
}
