using ProjectResourceManagement.Server.Data.Repositories;

using ProjectResourceManagement.Server.Security;

using ProjectResourceManagement.Shared.Constants;

using ProjectResourceManagement.Shared.DTOs.Auth;



namespace ProjectResourceManagement.Server.Services;



public sealed class AuthService(

    UserRepository userRepository,

    RbacRepository rbacRepository,

    IPasswordHasher passwordHasher,

    IJwtTokenService jwtTokenService)

{

    public async Task<AuthResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)

    {

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))

        {

            return AuthResult<LoginResponse>.Failure(AuthResultCode.InvalidCredentials, "Username and password are required.");

        }



        var user = await userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))

        {

            return AuthResult<LoginResponse>.Failure(AuthResultCode.InvalidCredentials, "Invalid username or password.");

        }



        if (!user.IsActive)

        {

            return AuthResult<LoginResponse>.Failure(AuthResultCode.InactiveUser, "User account is inactive.");

        }



        user.LastLoginAtUtc = DateTime.UtcNow;

        await userRepository.SaveChangesAsync(cancellationToken);



        var roles = await rbacRepository.GetRoleNamesForUserAsync(user.Id, cancellationToken);

        var permissions = await rbacRepository.GetPermissionCodesForUserAsync(user.Id, cancellationToken);

        var (accessToken, expiresAtUtc) = jwtTokenService.CreateAccessToken(user, roles);



        return AuthResult<LoginResponse>.Success(new LoginResponse(

            user.Id,

            user.Profile?.FullName ?? user.Username,

            user.Username,

            roles,

            permissions.OrderBy(code => code).ToList(),

            user.ForcePasswordChange,

            accessToken,

            expiresAtUtc));

    }



    public async Task<AuthResult<ChangePasswordResponse>> ChangePasswordAsync(

        int userId,

        ChangePasswordRequest request,

        CancellationToken cancellationToken = default)

    {

        if (request.NewPassword.Length < BusinessRules.MinimumPasswordLength)

        {

            return AuthResult<ChangePasswordResponse>.Failure(

                AuthResultCode.PasswordTooShort,

                $"Password must be at least {BusinessRules.MinimumPasswordLength} characters.");

        }



        if (request.NewPassword != request.ConfirmPassword)

        {

            return AuthResult<ChangePasswordResponse>.Failure(AuthResultCode.PasswordMismatch, "Password confirmation does not match.");

        }



        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)

        {

            return AuthResult<ChangePasswordResponse>.Failure(AuthResultCode.UserNotFound, "User was not found.");

        }



        if (!user.IsActive)

        {

            return AuthResult<ChangePasswordResponse>.Failure(AuthResultCode.InactiveUser, "User account is inactive.");

        }



        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        user.ForcePasswordChange = false;

        await userRepository.SaveChangesAsync(cancellationToken);



        return AuthResult<ChangePasswordResponse>.Success(new ChangePasswordResponse(user.Id, false));

    }

}

