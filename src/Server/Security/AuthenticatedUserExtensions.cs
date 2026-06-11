using System.Security.Claims;

namespace ProjectResourceManagement.Server.Security;

internal static class AuthenticatedUserExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return int.TryParse(rawUserId, out var userId) ? userId : null;
    }

    public static bool TryGetAuthenticatedUserId(
        this HttpContext context,
        out int userId,
        out string errorMessage)
    {
        userId = 0;
        var parsedUserId = context.User.GetUserId();
        if (parsedUserId is null)
        {
            errorMessage = "Missing or invalid authentication token.";
            return false;
        }

        userId = parsedUserId.Value;
        errorMessage = string.Empty;
        return true;
    }
}
