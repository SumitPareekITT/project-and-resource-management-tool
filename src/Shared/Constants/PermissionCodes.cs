namespace ProjectResourceManagement.Shared.Constants;

public static class PermissionCodes
{
    public const string UsersList = "users.list";
    public const string UsersCreate = "users.create";
    public const string UsersResetPassword = "users.reset_password";
    public const string UsersDeactivate = "users.deactivate";
    public const string UsersReactivate = "users.reactivate";

    public const string UserProfilesList = "user_profiles.list";
    public const string UserProfilesCreate = "user_profiles.create";
    public const string UserProfilesUpdate = "user_profiles.update";
    public const string UserProfilesDeactivate = "user_profiles.deactivate";
    public const string UserProfilesAssignManager = "user_profiles.assign_manager";
    public const string UserProfilesSkillsUpsert = "user_profiles.skills.upsert";
    public const string UserProfilesSkillsRemove = "user_profiles.skills.remove";

    public const string ProjectsList = "projects.list";
    public const string ProjectsCreate = "projects.create";
    public const string ProjectsUpdate = "projects.update";
    public const string ProjectsUpdateStatus = "projects.update_status";
    public const string ProjectsMilestonesManage = "projects.milestones.manage";

    public const string SkillsList = "skills.list";
    public const string SkillsCreate = "skills.create";
    public const string SkillsUpdate = "skills.update";
    public const string SkillsDeactivate = "skills.deactivate";

    public const string AllocationsMatrix = "allocations.matrix";

    public const string SystemConfigurationRead = "system_configuration.read";
    public const string SystemConfigurationUpdate = "system_configuration.update";

    public const string ManagerDashboardView = "manager.dashboard.view";
    public const string ManagerProjectsList = "manager.projects.list";
    public const string ManagerAllocationsCreate = "manager.allocations.create";
    public const string ManagerAllocationsEnd = "manager.allocations.end";
    public const string ManagerTimesheetsList = "manager.timesheets.list";
    public const string ManagerTimesheetsView = "manager.timesheets.view";
    public const string ManagerAiSkillMatch = "manager.ai.skill_match";
    public const string ManagerAiProjectRisk = "manager.ai.project_risk";

    public const string EmployeeTimesheetsSubmit = "employee.timesheets.submit";
    public const string EmployeeTimesheetsHistory = "employee.timesheets.history";
    public const string EmployeeTimesheetsView = "employee.timesheets.view";
    public const string EmployeeAllocationsView = "employee.allocations.view";
    public const string EmployeeActivityTagsList = "employee.activity_tags.list";
    public const string EmployeeMissingReminder = "employee.timesheets.missing_reminder";
}
