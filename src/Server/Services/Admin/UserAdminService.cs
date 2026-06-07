using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class UserAdminService(UserRepository userRepository, IPasswordHasher passwordHasher)
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

        if (await userRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return AdminResult<UserSummaryDto>.Fail(AdminResultCode.Conflict, "Email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Username = username,
            PasswordHash = passwordHasher.Hash(request.TemporaryPassword),
            Role = request.Role,
            ForcePasswordChange = true,
            IsActive = true
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return AdminResult<UserSummaryDto>.Success(MapUser(user));
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
        await userRepository.SaveChangesAsync(cancellationToken);

        return AdminResult<UserSummaryDto>.Success(MapUser(user), "User deactivated successfully.");
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
        return new UserSummaryDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Username,
            user.Role,
            user.ForcePasswordChange,
            user.IsActive);
    }
}
