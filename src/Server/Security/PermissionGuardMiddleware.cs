using ProjectResourceManagement.Server.Data.Repositories;

namespace ProjectResourceManagement.Server.Security;

public sealed class PermissionGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RbacRepository rbacRepository)
    {
        var requiredPermissions = context.GetEndpoint()?.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>()
            .Select(attribute => attribute.PermissionCode)
            .Distinct()
            .ToArray();

        if (requiredPermissions is null || requiredPermissions.Length == 0)
        {
            await next(context);
            return;
        }

        if (!context.TryGetAuthenticatedUserId(out var userId, out var errorMessage))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Message = errorMessage });
            return;
        }

        var userPermissions = await rbacRepository.GetPermissionCodesForUserAsync(userId, context.RequestAborted);
        if (!requiredPermissions.All(userPermissions.Contains))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = "You do not have permission for this operation." });
            return;
        }

        await next(context);
    }
}
