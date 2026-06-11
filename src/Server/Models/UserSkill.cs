using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class UserSkill
{
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public ProficiencyLevel ProficiencyLevel { get; set; } = ProficiencyLevel.Intermediate;
    public decimal? YearsOfExperience { get; set; }
    public DateOnly? LastUsedOn { get; set; }

    public User User { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
