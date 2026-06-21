using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProjectResourceManagement.Server.Swagger;

internal static class SwaggerConfiguration
{
    public static IServiceCollection AddPrmSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PRM Tool API",
                Version = "v1",
                Description = "Project & Resource Management Tool — browse and test APIs in the browser."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "1) Call POST /api/auth/login  2) Copy accessToken  3) Click Authorize and paste the token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<AnonymousOperationFilter>();
            options.TagActionsBy(api =>
            {
                if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
                    && !string.IsNullOrWhiteSpace(controller))
                {
                    return [MapControllerTag(controller)];
                }

                return ["other"];
            });
            options.DocInclusionPredicate((_, _) => true);
        });

        return services;
    }

    public static WebApplication UsePrmSwaggerUi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "PRM Tool API v1");
            options.RoutePrefix = "docs";
            options.DocumentTitle = "PRM Tool API - Swagger UI";
        });

        return app;
    }

    private static string MapControllerTag(string controller) => controller switch
    {
        "Auth" => "auth",
        "Users" => "admin — users",
        "UserProfiles" => "admin — user profiles",
        "Projects" => "admin — projects",
        "Skills" => "admin — skills",
        "Allocations" => "admin — allocations",
        "SystemConfiguration" => "admin — system config",
        "Manager" => "manager",
        "ManagerAi" => "manager — AI",
        "EmployeeTimesheets" => "employee",
        _ => controller.ToLowerInvariant()
    };

    private sealed class AnonymousOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var allowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
                || context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;

            if (allowAnonymous)
            {
                operation.Security.Clear();
            }
        }
    }
}
