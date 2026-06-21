using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;

namespace ProjectResourceManagement.Server.Data;

public static class RbacSeedData
{
    private static readonly DateTime SeededAtUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<Role> Roles { get; } =
    [
        new() { Id = 1, RoleName = "Admin", Description = "System administrator", IsActive = true },
        new() { Id = 2, RoleName = "Manager", Description = "Delivery manager", IsActive = true },
        new() { Id = 3, RoleName = "Employee", Description = "Individual contributor", IsActive = true }
    ];

    public static IReadOnlyList<Permission> Permissions { get; } =
    [
        new() { Id = 1, PermissionCode = PermissionCodes.UsersList, Description = "List user accounts", HttpMethod = "GET", RoutePattern = "/api/users" },
        new() { Id = 2, PermissionCode = PermissionCodes.UsersCreate, Description = "Create user account", HttpMethod = "POST", RoutePattern = "/api/users" },
        new() { Id = 3, PermissionCode = PermissionCodes.UsersResetPassword, Description = "Reset user password", HttpMethod = "PUT", RoutePattern = "/api/users/*/reset-password" },
        new() { Id = 4, PermissionCode = PermissionCodes.UsersDeactivate, Description = "Deactivate user", HttpMethod = "PUT", RoutePattern = "/api/users/*/deactivate" },
        new() { Id = 5, PermissionCode = PermissionCodes.UsersReactivate, Description = "Reactivate user", HttpMethod = "PUT", RoutePattern = "/api/users/*/reactivate" },

        new() { Id = 10, PermissionCode = PermissionCodes.UserProfilesList, Description = "List user profiles", HttpMethod = "GET", RoutePattern = "/api/user-profiles" },
        new() { Id = 11, PermissionCode = PermissionCodes.UserProfilesCreate, Description = "Create user profile", HttpMethod = "POST", RoutePattern = "/api/user-profiles" },
        new() { Id = 12, PermissionCode = PermissionCodes.UserProfilesUpdate, Description = "Update user profile", HttpMethod = "PUT", RoutePattern = "/api/user-profiles/*" },
        new() { Id = 13, PermissionCode = PermissionCodes.UserProfilesDeactivate, Description = "Deactivate user profile", HttpMethod = "PUT", RoutePattern = "/api/user-profiles/*/deactivate" },
        new() { Id = 14, PermissionCode = PermissionCodes.UserProfilesAssignManager, Description = "Assign manager", HttpMethod = "PUT", RoutePattern = "/api/user-profiles/assign-manager" },
        new() { Id = 15, PermissionCode = PermissionCodes.UserProfilesSkillsUpsert, Description = "Upsert profile skill", HttpMethod = "POST", RoutePattern = "/api/user-profiles/*/skills" },
        new() { Id = 16, PermissionCode = PermissionCodes.UserProfilesSkillsRemove, Description = "Remove profile skill", HttpMethod = "DELETE", RoutePattern = "/api/user-profiles/*/skills/*" },

        new() { Id = 20, PermissionCode = PermissionCodes.ProjectsList, Description = "List projects", HttpMethod = "GET", RoutePattern = "/api/projects" },
        new() { Id = 21, PermissionCode = PermissionCodes.ProjectsCreate, Description = "Create project", HttpMethod = "POST", RoutePattern = "/api/projects" },
        new() { Id = 22, PermissionCode = PermissionCodes.ProjectsUpdate, Description = "Update project", HttpMethod = "PUT", RoutePattern = "/api/projects/*" },
        new() { Id = 23, PermissionCode = PermissionCodes.ProjectsUpdateStatus, Description = "Update project status", HttpMethod = "PUT", RoutePattern = "/api/projects/*/status" },
        new() { Id = 24, PermissionCode = PermissionCodes.ProjectsMilestonesManage, Description = "Manage milestones", HttpMethod = "POST", RoutePattern = "/api/projects/*/milestones" },

        new() { Id = 30, PermissionCode = PermissionCodes.SkillsList, Description = "List skills", HttpMethod = "GET", RoutePattern = "/api/skills" },
        new() { Id = 31, PermissionCode = PermissionCodes.SkillsCreate, Description = "Create skill", HttpMethod = "POST", RoutePattern = "/api/skills" },
        new() { Id = 32, PermissionCode = PermissionCodes.SkillsUpdate, Description = "Update skill", HttpMethod = "PUT", RoutePattern = "/api/skills/*" },
        new() { Id = 33, PermissionCode = PermissionCodes.SkillsDeactivate, Description = "Deactivate skill", HttpMethod = "PUT", RoutePattern = "/api/skills/*/deactivate" },

        new() { Id = 40, PermissionCode = PermissionCodes.AllocationsMatrix, Description = "View allocation matrix", HttpMethod = "GET", RoutePattern = "/api/allocations/matrix" },

        new() { Id = 50, PermissionCode = PermissionCodes.SystemConfigurationRead, Description = "Read system config", HttpMethod = "GET", RoutePattern = "/api/system-configuration" },
        new() { Id = 51, PermissionCode = PermissionCodes.SystemConfigurationUpdate, Description = "Update system config", HttpMethod = "PUT", RoutePattern = "/api/system-configuration" },

        new() { Id = 60, PermissionCode = PermissionCodes.ManagerDashboardView, Description = "View manager dashboard", HttpMethod = "GET", RoutePattern = "/api/manager/dashboard" },
        new() { Id = 61, PermissionCode = PermissionCodes.ManagerProjectsList, Description = "List manager projects", HttpMethod = "GET", RoutePattern = "/api/manager/projects" },
        new() { Id = 62, PermissionCode = PermissionCodes.ManagerAllocationsCreate, Description = "Create allocation", HttpMethod = "POST", RoutePattern = "/api/manager/allocations" },
        new() { Id = 63, PermissionCode = PermissionCodes.ManagerAllocationsEnd, Description = "End allocation", HttpMethod = "PUT", RoutePattern = "/api/manager/allocations/*/end" },
        new() { Id = 64, PermissionCode = PermissionCodes.ManagerTimesheetsList, Description = "List team timesheets", HttpMethod = "GET", RoutePattern = "/api/manager/timesheets" },
        new() { Id = 65, PermissionCode = PermissionCodes.ManagerTimesheetsView, Description = "View team timesheet", HttpMethod = "GET", RoutePattern = "/api/manager/timesheets/*" },
        new() { Id = 66, PermissionCode = PermissionCodes.ManagerAiSkillMatch, Description = "AI skill match", HttpMethod = "POST", RoutePattern = "/api/manager/ai/skill-match" },
        new() { Id = 67, PermissionCode = PermissionCodes.ManagerAiProjectRisk, Description = "AI project risk", HttpMethod = "POST", RoutePattern = "/api/manager/ai/project-risk-summary" },
        new() { Id = 68, PermissionCode = PermissionCodes.ManagerAiTeamMatch, Description = "AI organization team match", HttpMethod = "POST", RoutePattern = "/api/manager/ai/team-match" },

        new() { Id = 70, PermissionCode = PermissionCodes.EmployeeTimesheetsSubmit, Description = "Submit timesheet", HttpMethod = "POST", RoutePattern = "/api/employee/timesheets" },
        new() { Id = 71, PermissionCode = PermissionCodes.EmployeeTimesheetsHistory, Description = "Timesheet history", HttpMethod = "GET", RoutePattern = "/api/employee/timesheets/history" },
        new() { Id = 72, PermissionCode = PermissionCodes.EmployeeTimesheetsView, Description = "View timesheet", HttpMethod = "GET", RoutePattern = "/api/employee/timesheets/*" },
        new() { Id = 73, PermissionCode = PermissionCodes.EmployeeAllocationsView, Description = "View allocations", HttpMethod = "GET", RoutePattern = "/api/employee/allocations" },
        new() { Id = 74, PermissionCode = PermissionCodes.EmployeeActivityTagsList, Description = "List activity tags", HttpMethod = "GET", RoutePattern = "/api/employee/activity-tags" },
        new() { Id = 75, PermissionCode = PermissionCodes.EmployeeMissingReminder, Description = "Missing timesheet reminder", HttpMethod = "GET", RoutePattern = "/api/employee/timesheets/missing-reminder" }
    ];

    public static IReadOnlyList<RolePermission> RolePermissions { get; } =
    [
        .. Permissions.Where(p => p.Id < 60).Select(p => new RolePermission { RoleId = 1, PermissionId = p.Id }),
        .. Permissions.Where(p => p.Id is >= 60 and <= 68).Select(p => new RolePermission { RoleId = 2, PermissionId = p.Id }),
        .. Permissions.Where(p => p.Id is >= 70 and <= 75).Select(p => new RolePermission { RoleId = 3, PermissionId = p.Id })
    ];

    public static IReadOnlyList<UserProfile> UserProfiles { get; } =
    [
        new()
        {
            Id = 1,
            UserId = 1,
            FullName = "System Admin",
            Email = "admin@techserve.local",
            Department = "Management",
            Designation = "System Administrator",
            ResourceStatus = Shared.Enums.EmployeeStatus.Inactive,
            CurrentUtilizationPercent = 0,
            IsActive = true,
            CreatedAtUtc = SeededAtUtc
        }
    ];

    public static IReadOnlyList<UserRoleAssignment> UserRoleAssignments { get; } =
    [
        new() { UserId = 1, RoleId = 1, AssignedAtUtc = SeededAtUtc }
    ];
}
