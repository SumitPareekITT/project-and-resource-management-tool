using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectResourceManagement.Server.Security;
using ProjectResourceManagement.Server.Services;
using ProjectResourceManagement.Shared.DTOs.Auth;

namespace ProjectResourceManagement.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!HttpContext.TryGetAuthenticatedUserId(out var userId, out var errorMessage))
        {
            return Unauthorized(new { Message = errorMessage });
        }

        var result = await authService.ChangePasswordAsync(userId, request, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(AuthResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Code switch
        {
            AuthResultCode.InvalidCredentials => Unauthorized(new { result.Message }),
            AuthResultCode.InactiveUser => StatusCode(StatusCodes.Status403Forbidden, new { result.Message }),
            AuthResultCode.UserNotFound => NotFound(new { result.Message }),
            AuthResultCode.PasswordTooShort or AuthResultCode.PasswordMismatch => BadRequest(new { result.Message }),
            _ => BadRequest(new { result.Message })
        };
    }
}
