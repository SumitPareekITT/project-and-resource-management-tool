using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class UserProfileAdminService(
    UserProfileRepository userProfileRepository,
    UserRepository userRepository,
    SkillRepository skillRepository,
    AllocationRepository allocationRepository,
    RbacRepository rbacRepository)
{
    public async Task<AdminResult<IReadOnlyList<UserProfileSummaryDto>>> ListProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await userProfileRepository.ListAsync(cancellationToken);
        return AdminResult<IReadOnlyList<UserProfileSummaryDto>>.Success(profiles.Select(MapProfile).ToList());
    }

    public async Task<AdminResult<UserProfileSummaryDto>> CreateProfileAsync(
        CreateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateProfileFields(request.FullName, request.Email, request.Department, request.Designation);
        if (validationError is not null)
        {
            return validationError;
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User account was not found.");
        }

        if (user.Profile is not null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.Conflict, "User profile already exists.");
        }

        var profileByEmail = await userProfileRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (profileByEmail is not null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.Conflict, "Email is already used by another profile.");
        }

        var managerValidation = await ValidateManagerAsync(request.UserId, request.ManagerUserId, cancellationToken);
        if (managerValidation is not null)
        {
            return managerValidation;
        }

        var profile = new UserProfile
        {
            UserId = request.UserId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Department = request.Department.Trim(),
            Designation = request.Designation.Trim(),
            ManagerUserId = request.ManagerUserId,
            ResourceStatus = EmployeeStatus.Bench,
            IsActive = true
        };

        await userProfileRepository.AddAsync(profile, cancellationToken);
        await userProfileRepository.SaveChangesAsync(cancellationToken);

        var created = await userProfileRepository.GetByIdAsync(profile.Id, cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(created!));
    }

    public async Task<AdminResult<UserProfileSummaryDto>> UpdateProfileAsync(
        int profileId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await userProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var validationError = ValidateProfileFields(request.FullName, request.Email, request.Department, request.Designation);
        if (validationError is not null)
        {
            return validationError;
        }

        var managerValidation = await ValidateManagerAsync(profile.UserId, request.ManagerUserId, cancellationToken);
        if (managerValidation is not null)
        {
            return managerValidation;
        }

        var profileByEmail = await userProfileRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (profileByEmail is not null && profileByEmail.Id != profile.Id)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.Conflict, "Email is already used by another profile.");
        }

        profile.FullName = request.FullName.Trim();
        profile.Email = request.Email.Trim();
        profile.Department = request.Department.Trim();
        profile.Designation = request.Designation.Trim();
        profile.ManagerUserId = request.ManagerUserId;

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(profile));
    }

    public async Task<AdminResult<UserProfileSummaryDto>> AssignManagerAsync(
        AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == request.ManagerUserId)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "User and manager cannot be the same.");
        }

        var profile = await userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var managerValidation = await ValidateManagerAsync(request.UserId, request.ManagerUserId, cancellationToken);
        if (managerValidation is not null)
        {
            return managerValidation;
        }

        profile.ManagerUserId = request.ManagerUserId;
        await userProfileRepository.SaveChangesAsync(cancellationToken);

        var updated = await userProfileRepository.GetByIdAsync(profile.Id, cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(updated!));
    }

    public async Task<AdminResult<UserProfileSummaryDto>> DeactivateProfileAsync(int profileId, CancellationToken cancellationToken = default)
    {
        var profile = await userProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        if (!profile.IsActive)
        {
            return AdminResult<UserProfileSummaryDto>.Success(MapProfile(profile), "Profile is already inactive.");
        }

        var utcNow = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        profile.IsActive = false;
        profile.ResourceStatus = EmployeeStatus.Inactive;
        profile.CurrentUtilizationPercent = 0;
        profile.DeactivatedAtUtc = utcNow;
        profile.User.IsActive = false;
        profile.User.DeactivatedAtUtc = utcNow;

        var activeAllocations = await allocationRepository.ListActiveByUserIdAsync(profile.UserId, cancellationToken);
        foreach (var allocation in activeAllocations)
        {
            allocation.Status = AllocationStatus.Ended;
            if (allocation.ToDate is null || allocation.ToDate > today)
            {
                allocation.ToDate = today;
            }
        }

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(profile), "User profile deactivated successfully.");
    }

    public async Task<AdminResult<UserProfileSummaryDto>> UpsertUserSkillAsync(
        int profileId,
        UpsertUserSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.YearsOfExperience is < 0)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Years of experience cannot be negative.");
        }

        var profile = await userProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var skill = await skillRepository.GetByIdAsync(request.SkillId, cancellationToken);
        if (skill is null || !skill.IsActive)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Skill must exist and be active.");
        }

        var existing = profile.User.Skills.FirstOrDefault(skillLink => skillLink.SkillId == request.SkillId);
        if (existing is null)
        {
            profile.User.Skills.Add(new UserSkill
            {
                UserId = profile.UserId,
                SkillId = request.SkillId,
                ProficiencyLevel = request.ProficiencyLevel,
                YearsOfExperience = request.YearsOfExperience,
                LastUsedOn = request.LastUsedOn
            });
        }
        else
        {
            existing.ProficiencyLevel = request.ProficiencyLevel;
            existing.YearsOfExperience = request.YearsOfExperience;
            existing.LastUsedOn = request.LastUsedOn;
        }

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        var updated = await userProfileRepository.GetByIdAsync(profileId, cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(updated!));
    }

    public async Task<AdminResult<UserProfileSummaryDto>> AddOrUpdateUserSkillByNameAsync(
        int userId,
        AddUserSkillByNameRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SkillName))
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Skill name is required.");
        }

        if (request.YearsOfExperience is < 0)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Years of experience cannot be negative.");
        }

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "Employee profile was not found for this user ID.");
        }

        var skill = await ResolveOrCreateSkillAsync(request.SkillName, request.Category, cancellationToken);
        if (skill is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Skill must exist and be active.");
        }

        var existing = profile.User.Skills.FirstOrDefault(skillLink => skillLink.SkillId == skill.Id);
        if (existing is null)
        {
            profile.User.Skills.Add(new UserSkill
            {
                UserId = profile.UserId,
                SkillId = skill.Id,
                ProficiencyLevel = request.ProficiencyLevel,
                YearsOfExperience = request.YearsOfExperience,
                LastUsedOn = request.LastUsedOn
            });
        }
        else
        {
            existing.ProficiencyLevel = request.ProficiencyLevel;
            existing.YearsOfExperience = request.YearsOfExperience;
            existing.LastUsedOn = request.LastUsedOn;
        }

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        var updated = await userProfileRepository.GetByIdAsync(profile.Id, cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(updated!));
    }

    public async Task<AdminResult<UserProfileSummaryDto>> RemoveUserSkillAsync(
        int profileId,
        int skillId,
        CancellationToken cancellationToken = default)
    {
        var profile = await userProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User profile was not found.");
        }

        var existing = profile.User.Skills.FirstOrDefault(skillLink => skillLink.SkillId == skillId);
        if (existing is null)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.NotFound, "User skill mapping was not found.");
        }

        profile.User.Skills.Remove(existing);
        await userProfileRepository.SaveChangesAsync(cancellationToken);

        var updated = await userProfileRepository.GetByIdAsync(profileId, cancellationToken);
        return AdminResult<UserProfileSummaryDto>.Success(MapProfile(updated!));
    }

    private async Task<Skill?> ResolveOrCreateSkillAsync(
        string skillName,
        SkillCategory category,
        CancellationToken cancellationToken)
    {
        var normalizedName = skillName.Trim();
        var existing = await skillRepository.GetByNameAsync(normalizedName, cancellationToken);
        if (existing is not null)
        {
            return existing.IsActive ? existing : null;
        }

        var skill = new Skill
        {
            Name = normalizedName,
            Category = category,
            IsActive = true
        };

        await skillRepository.AddAsync(skill, cancellationToken);
        await skillRepository.SaveChangesAsync(cancellationToken);
        return skill;
    }

    private static AdminResult<UserProfileSummaryDto>? ValidateProfileFields(string fullName, string email, string department, string designation)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Department is required.");
        }

        if (string.IsNullOrWhiteSpace(designation))
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Designation is required.");
        }

        return null;
    }

    private async Task<AdminResult<UserProfileSummaryDto>?> ValidateManagerAsync(
        int userId,
        int? managerUserId,
        CancellationToken cancellationToken)
    {
        if (managerUserId is null)
        {
            return null;
        }

        if (userId == managerUserId.Value)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "User cannot report to self.");
        }

        var managerUser = await userRepository.GetByIdAsync(managerUserId.Value, cancellationToken);
        if (managerUser is null || !managerUser.IsActive)
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Manager must be an active user.");
        }

        var managerRoles = await rbacRepository.GetRoleNamesForUserAsync(managerUserId.Value, cancellationToken);
        if (!managerRoles.Contains(nameof(UserRole.Manager), StringComparer.OrdinalIgnoreCase))
        {
            return AdminResult<UserProfileSummaryDto>.Fail(AdminResultCode.ValidationError, "Manager must have Manager role.");
        }

        return null;
    }

    private static UserProfileSummaryDto MapProfile(UserProfile profile)
    {
        var skills = profile.User.Skills
            .OrderBy(skill => skill.Skill.Name)
            .Select(skill => new UserSkillDto(
                skill.SkillId,
                skill.Skill.Name,
                skill.Skill.Category,
                skill.ProficiencyLevel,
                skill.YearsOfExperience,
                skill.LastUsedOn))
            .ToList();

        var roles = profile.User.RoleAssignments
            .Select(assignment => assignment.Role.RoleName)
            .OrderBy(name => name)
            .ToList();

        return new UserProfileSummaryDto(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.Email,
            profile.Department,
            profile.Designation,
            profile.ResourceStatus,
            profile.CurrentUtilizationPercent,
            profile.IsActive,
            profile.ManagerUserId,
            profile.ManagerUser?.Profile?.FullName,
            roles,
            skills);
    }
}
