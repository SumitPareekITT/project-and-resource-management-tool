using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Security;

public sealed class RoleGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requiredRoles = context.GetEndpoint()?.Metadata.GetOrderedMetadata<RequireRoleAttribute>()
            .SelectMany(attribute => attribute.AllowedRoles)
            .Distinct()
            .ToArray();

        if (requiredRoles is null || requiredRoles.Length == 0)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-User-Role", out var rawRole) ||
            !Enum.TryParse<UserRole>(rawRole.ToString(), ignoreCase: true, out var parsedRole))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                Message = "Missing or invalid X-User-Role header."
            });
            return;
        }

        if (!requiredRoles.Contains(parsedRole))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                Message = "You do not have permission for this operation."
            });
            return;
        }

        await next(context);
    }
}
