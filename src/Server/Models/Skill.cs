using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Models;

public sealed class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; } = SkillCategory.Other;
    public bool IsActive { get; set; } = true;

    public ICollection<UserSkill> Users { get; set; } = new List<UserSkill>();
}
