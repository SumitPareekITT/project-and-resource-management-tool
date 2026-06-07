using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.DTOs.Admin;

namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class SkillAdminService(SkillRepository skillRepository)
{
    public async Task<AdminResult<IReadOnlyList<SkillDto>>> ListSkillsAsync(CancellationToken cancellationToken = default)
    {
        var skills = await skillRepository.ListAsync(cancellationToken);
        var mapped = skills
            .Select(skill => new SkillDto(skill.Id, skill.Name, skill.Category, skill.IsActive))
            .ToList();
        return AdminResult<IReadOnlyList<SkillDto>>.Success(mapped);
    }

    public async Task<AdminResult<SkillDto>> CreateSkillAsync(UpsertSkillRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedName = request.Name.Trim();
        var existing = await skillRepository.GetByNameAsync(normalizedName, cancellationToken);
        if (existing is not null)
        {
            return AdminResult<SkillDto>.Fail(AdminResultCode.Conflict, "Skill with this name already exists.");
        }

        var skill = new Skill
        {
            Name = normalizedName,
            Category = request.Category,
            IsActive = true
        };

        await skillRepository.AddAsync(skill, cancellationToken);
        await skillRepository.SaveChangesAsync(cancellationToken);

        return AdminResult<SkillDto>.Success(new SkillDto(skill.Id, skill.Name, skill.Category, skill.IsActive));
    }

    public async Task<AdminResult<SkillDto>> UpdateSkillAsync(int id, UpsertSkillRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var skill = await skillRepository.GetByIdAsync(id, cancellationToken);
        if (skill is null)
        {
            return AdminResult<SkillDto>.Fail(AdminResultCode.NotFound, "Skill was not found.");
        }

        var normalizedName = request.Name.Trim();
        var byName = await skillRepository.GetByNameAsync(normalizedName, cancellationToken);
        if (byName is not null && byName.Id != id)
        {
            return AdminResult<SkillDto>.Fail(AdminResultCode.Conflict, "Skill with this name already exists.");
        }

        skill.Name = normalizedName;
        skill.Category = request.Category;

        await skillRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<SkillDto>.Success(new SkillDto(skill.Id, skill.Name, skill.Category, skill.IsActive));
    }

    public async Task<AdminResult<SkillDto>> DeactivateSkillAsync(int id, CancellationToken cancellationToken = default)
    {
        var skill = await skillRepository.GetByIdAsync(id, cancellationToken);
        if (skill is null)
        {
            return AdminResult<SkillDto>.Fail(AdminResultCode.NotFound, "Skill was not found.");
        }

        if (!skill.IsActive)
        {
            return AdminResult<SkillDto>.Success(new SkillDto(skill.Id, skill.Name, skill.Category, false), "Skill is already inactive.");
        }

        skill.IsActive = false;
        await skillRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<SkillDto>.Success(new SkillDto(skill.Id, skill.Name, skill.Category, false), "Skill deactivated.");
    }

    private static AdminResult<SkillDto>? ValidateRequest(UpsertSkillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return AdminResult<SkillDto>.Fail(AdminResultCode.ValidationError, "Skill name is required.");
        }

        return null;
    }
}
