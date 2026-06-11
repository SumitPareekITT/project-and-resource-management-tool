using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class UserAdminService(
    UserRepository userRepository,
    UserProfileRepository userProfileRepository,
    RbacRepository rbacRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<AdminResult<IReadOnlyList<UserSummaryDto>>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        var mapped = users.Select(MapUser).ToList();
        return AdminResult<IReadOnlyList<UserSummaryDto>>.Success(mapped);
    }

    public async Task<AdminResult<UserSummaryDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await userRepository.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.Conflict, "Username already exists.");
        }

        if (await userProfileRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.Conflict, "Email already exists.");
        }

        var role = await rbacRepository.GetRoleByNameAsync(request.Role.ToString(), cancellationToken);
        if (role is null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Role is not configured in the database.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = passwordHasher.Hash(request.TemporaryPassword),
            ForcePasswordChange = true,
            IsActive = true
        };

        var profile = new UserProfile
        {
            User = user,
            FullName = request.FullName.Trim(),
            Email = email,
            Department = request.Department.Trim(),
            Designation = request.Designation.Trim(),
            ManagerUserId = request.ManagerUserId,
            ResourceStatus = request.Role == UserRole.Admin ? EmployeeStatus.Inactive : EmployeeStatus.Bench,
            CurrentUtilizationPercent = 0,
            IsActive = true
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userProfileRepository.AddAsync(profile, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        await rbacRepository.AssignRoleAsync(user.Id, role.Id, cancellationToken);
        await rbacRepository.SaveChangesAsync(cancellationToken);

        var created = await userRepository.GetByIdAsync(user.Id, cancellationToken);
        return AdminResult<UserSummaryDto>.Success(MapUser(created!));
    }

    public async Task<AdminResult<UserSummaryDto>> ResetPasswordAsync(
        int userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.NewPassword.Length < BusinessRules.MinimumPasswordLength)
        {
            return AdminResult<UserSummaryDto>.Fail(
                AdminResultCode.ValidationError,
                $"Password must be at least {BusinessRules.MinimumPasswordLength} characters.");
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.NotFound, "User was not found.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.ForcePasswordChange = true;
        await userRepository.SaveChangesAsync(cancellationToken);

        return AdminResult<UserSummaryDto>.Success(MapUser(user), "Password reset successfully.");
    }

    public async Task<AdminResult<UserSummaryDto>> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.NotFound, "User was not found.");
        }

        if (!user.IsActive)
        {
            return AdminResult<UserSummaryDto>.Success(MapUser(user), "User is already inactive.");
        }

        user.IsActive = false;
        user.DeactivatedAtUtc = DateTime.UtcNow;
        if (user.Profile is not null)
        {
            user.Profile.IsActive = false;
            user.Profile.DeactivatedAtUtc = user.DeactivatedAtUtc;
        }

        await userRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<UserSummaryDto>.Success(MapUser(user), "User deactivated successfully.");
    }

    public async Task<AdminResult<UserSummaryDto>> ReactivateUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.NotFound, "User was not found.");
        }

        if (user.IsActive)
        {
            return AdminResult<UserSummaryDto>.Success(MapUser(user), "User is already active.");
        }

        user.IsActive = true;
        user.DeactivatedAtUtc = null;
        if (user.Profile is not null)
        {
            user.Profile.IsActive = true;
            user.Profile.DeactivatedAtUtc = null;
        }

        await userRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<UserSummaryDto>.Success(MapUser(user), "User reactivated successfully.");
    }

    private static AdminResult<UserSummaryDto>? ValidateCreateRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Department))
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Department is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Designation))
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Designation is required.");
        }

        if (request.TemporaryPassword.Length < BusinessRules.MinimumPasswordLength)
        {
            return AdminResult<UserSummaryDto>.Fail(
                AdminResultCode.ValidationError,
                $"Temporary password must be at least {BusinessRules.MinimumPasswordLength} characters.");
        }

        if (request.Role == UserRole.Admin)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.ValidationError, "Admin users cannot be created through this API.");
        }

        return null;
    }

    private static UserSummaryDto MapUser(User user)
    {
        var roles = user.RoleAssignments
            .Select(assignment => assignment.Role.RoleName)
            .OrderBy(name => name)
            .ToList();

        return new UserSummaryDto(
            user.Id,
            user.Profile?.FullName ?? user.Username,
            user.Profile?.Email ?? string.Empty,
            user.Username,
            roles,
            user.ForcePasswordChange,
            user.IsActive);
    }
}
