# Project & Resource Management Tool - Tracker

Last updated: 2026-06-05

## Source Documents

- BRD: `C:\Users\sumit.pareek\Downloads\PRM_BRD_V4.md`
- Previous BRD: `C:\Users\sumit.pareek\Downloads\PRM_BRD_V3.md`
- Design document: `C:\Users\sumit.pareek\Downloads\DesignDocument-ProjectResourceManagement-LnC.pdf`

Note: The BRD is the primary implementation source. The PDF appears to contain architecture/use-case/sequence diagrams for authentication, user management, admin, manager, employee, resource allocation, timesheet, and project health flows. Text extraction from the PDF was limited, so it should be treated as a visual reference unless exported to text/images later.

## Current Repo State

- The repository has a valid .NET solution and project files.
- `src/Server`, `src/Client`, `src/Shared`, and `tests/Server.Tests` are wired into the solution.
- Server, client, and tests reference shared contracts where needed.
- Shared enums and initial auth DTOs are implemented.
- Server domain entities and EF Core `ApplicationDbContext` are implemented.
- Initial repositories are implemented and registered in DI.
- Root `Program.cs` is empty in the working tree and is not part of the solution.
- Server `appsettings.json` contains a MySQL-style connection string for `prm_db`.
- Local .NET SDK is available.

## V4 BRD Changes Noted

- Admin Manage Employees no longer includes an Add Employee screen in the console flow.
- Admin Manage Employees now includes Assign Manager.
- Employee records need `manager_id`, linked by Employee User ID and Manager User ID.
- Manager visibility is scoped to their direct team:
  - Resource dashboard shows only assigned team members.
  - Resource search/allocation is limited to assigned team members.
- Project creation now includes `Total Story Points`.
- Project list now shows `SP Done/Total`.
- Update Project Details is now an explicit screen and supports editing project name, dates, status, manager, and total story points.
- Project and milestone modeling must support story-point progress for project health.

## Product Summary

The PRM Tool is a console-based client-server application for service-based IT project/resource planning. It replaces spreadsheet-driven planning with a single system for employees, skills, projects, allocations, milestones, timesheets, and AI-assisted recommendations.

Required applications:

- REST API server.
- Console client consuming the REST APIs.
- Shared contracts/enums/DTOs.
- Server-side tests.

Optional web/desktop UI is out of scope.

## Roles

### Admin

- Create and manage user accounts.
- Add/update/deactivate employee profiles.
- Assign employees to managers.
- Assign and manage employee skills.
- Create/update projects and milestones.
- View company-wide allocation matrix.
- Configure system settings such as LLM key, scheduler interval, and max weekly hours.
- Reset passwords and deactivate users.
- Cannot allocate resources or view timesheets.

### Manager

- Search available employees with natural language AI queries.
- Allocate employees to projects with utilization percentage and date range.
- Monitor project health.
- View team timesheets read-only.
- Use AI skill matching and AI risk summaries.
- Cannot modify employee profiles or system settings.

### Employee

- Submit weekly timesheets.
- Tag work activity, which becomes evidence for skill matching.
- View own allocation and timesheet history.
- Cannot view other employees, projects, or allocations.

## Core Business Rules

- First admin is seeded directly with default credentials and must change password on first login.
- Admin-created users have `force_password_change = true`.
- Employee profile and user account are separate records linked by `employee.user_id`.
- Employee profile can be linked only to an existing Manager or Employee user.
- Deactivating an employee ends active allocations immediately and blocks login.
- Allocation rules must prevent over-allocation beyond configured capacity.
- Timesheet rules:
  - Employee can log only against active allocations for that week.
  - Project hours cannot exceed allocation percentage times max weekly hours.
  - Total weekly hours cannot exceed configured max weekly hours.
  - Duplicate weekly timesheets are blocked.
  - Future-week submission is blocked.
- AI must rank/explain from system-selected data only; AI does not directly query the database.

## Planned Architecture

- `src/Server`: ASP.NET Core Web API.
- `src/Client`: .NET console application.
- `src/Shared`: shared DTOs/enums/contracts used by client and server.
- `tests/Server.Tests`: xUnit tests for server services.

Recommended patterns and principles:

- Repository pattern for persistence boundaries.
- Service layer for business rules.
- Adapter/strategy pattern for AI providers such as Gemini and Groq.
- Separation of concerns between console UI, API clients, controllers, services, repositories, and data models.
- Fail fast validation in services.

## Main Domain Model Draft

- User
- Employee
- Skill
- EmployeeSkill
- Project
- Milestone
- Allocation
- Timesheet
- TimesheetEntry
- ActivityTag
- SystemConfiguration

## Feature Backlog

### Phase 1 - Project Foundation

- [x] Create valid solution and project files.
- [x] Wire project references.
- [x] Add server, client, shared, and test project dependencies.
- [x] Move app settings into server project.
- [x] Add initial README/project documentation.
- [x] Add `.gitignore`.
- [x] Confirm restore, build, and test commands work.

### Phase 2 - Shared Contracts and Domain

- [x] Implement enums.
- [x] Implement core models/entities.
- [x] Implement auth DTOs and first request/response DTOs.
- [x] Define common validation constants.

### Phase 3 - Server Data Layer

- [x] Add EF Core DbContext.
- [x] Configure relationships and constraints.
- [x] Add repositories.
- [x] Add seed data for first admin and baseline activity tags/system settings.

### Phase 4 - Authentication and Users

- Implement login.
- Implement forced password change.
- Implement password reset and user deactivation.
- Add role-based authorization.

### Phase 5 - Admin Features

- Employee CRUD and deactivation.
- Skill management.
- Project and milestone management.
- User management.
- System configuration.
- Company allocation view.

### Phase 6 - Manager Features

- Resource dashboard.
- Allocation creation/update/end flows.
- My projects and project health.
- Team timesheet read-only views.

### Phase 7 - Employee Features

- Timesheet submission.
- Timesheet history and details.
- My allocation history.
- Missing timesheet reminder.

### Phase 8 - Scheduler and AI

- Utilization computation job.
- Project health flagging job.
- AI skill match flow.
- AI project risk summary flow.
- AI provider abstraction for Gemini/Groq.

### Phase 9 - Tests and Documentation

- Unit tests for business rules.
- Integration-style tests for key API flows where practical.
- Document SOLID examples, design patterns, and design principles.
- Add setup/run instructions.

## Immediate Next Step

Continue Day 3/Day 4 by adding Admin user-management APIs: create user, list users, reset password, deactivate user, and role checks.

## 10-Day Delivery Plan

Goal: complete an end-to-end BRD-aligned console client + REST server project with documentation, tests, and demonstrable design principles.

### Day 1 - Foundation and Build Setup

- [x] Create valid `.sln` and `.csproj` files for Shared, Server, Client, and Server.Tests.
- [x] Add project references.
- [x] Choose target framework and core packages.
- [x] Move configuration into the server project.
- [x] Confirm `dotnet build` works.
- [x] Update README with project overview and run plan.

Deliverable: buildable empty application structure.

### Day 2 - Shared Domain and Database Model

- [x] Implement shared enums.
- [x] Implement server domain entities for users, employees, skills, projects, milestones, allocations, timesheets, activity tags, and system configuration.
- [x] Create EF Core `ApplicationDbContext`.
- [x] Configure core relationships and constraints.
- [x] Add seed data for first admin, system defaults, and baseline activity tags.
- [x] Add common validation constants.
- [x] Add initial repositories.

Deliverable: domain model and database context ready for services.

### Day 3 - Authentication and User Management

- [x] Implement password hashing.
- [x] Implement login API.
- [x] Implement forced password change.
- [ ] Implement create user, view users, reset password, and deactivate user.
- [ ] Add role checks for Admin, Manager, and Employee flows.
- [x] Add tests for login, first-login password change, and deactivated user login blocking.

Deliverable: secure enough authentication and user-management foundation.

### Day 4 - Admin Employee and Skill Management

- Implement employee create/view/update/deactivate APIs.
- Implement employee-user linking validation.
- Implement employee skill add/update/remove flows.
- Enforce deactivation rules for employee allocations and linked login.
- Create matching console screens and API clients.
- Add service tests for employee validation and deactivation behavior.

Deliverable: Admin can manage people and skills end to end from console.

### Day 5 - Admin Project and Milestone Management

- Implement project create/view/update/status APIs.
- Implement milestone add/update/status APIs.
- Add Admin allocation matrix read-only view.
- Create matching console screens and API clients.
- Add tests for project and milestone rules.

Deliverable: Admin can manage project master data and milestones.

### Day 6 - Allocation and Resource Dashboard

- Implement allocation service and repository.
- Enforce no over-allocation beyond configured max capacity.
- Implement manager resource dashboard with bench/allocated/overallocated views.
- Implement allocate resource flow with project, employee, utilization, and date range.
- Implement end/update allocation as needed by BRD screens.
- Add tests for overlap, capacity, and date validation.

Deliverable: Manager can view resources and allocate people safely.

### Day 7 - Timesheets

- Implement employee timesheet submission API.
- Enforce allocation-week validation, per-project hour limits, total weekly max, duplicate prevention, and no future submission.
- Implement employee timesheet history/detail APIs.
- Implement manager read-only team timesheet view.
- Implement missing timesheet reminder logic.
- Add tests for all major timesheet rules.

Deliverable: Employee and Manager timesheet flows work end to end.

### Day 8 - Scheduler and Project Health

- Implement utilization computation job.
- Implement project health flagging based on milestones, allocation, and recent timesheets.
- Add project health fields/status to project views.
- Implement Manager "My Projects" health screen.
- Add service tests for health status and utilization calculation.

Deliverable: project health and utilization are computed automatically.

### Day 9 - AI Assistant

- Implement AI provider abstraction.
- Add Gemini/Groq adapter stubs or live adapters depending on available keys.
- Implement AI skill match flow using system-filtered candidate data.
- Implement AI risk summary flow using project facts.
- Add graceful fallback when no API key is configured.
- Add console screens for Manager AI Assistant.
- Add tests around candidate pre-filtering and prompt input shaping.

Deliverable: BRD AI features work with provider abstraction and safe fallback.

### Day 10 - Polish, Testing, and Documentation

- Run full build and test suite.
- Fix integration issues across client/server contracts.
- Improve console UX consistency with BRD screen layouts.
- Complete README setup/run instructions.
- Document SOLID principles, design patterns, and design principles used.
- Add sample demo flow and credentials.
- Final smoke test: Admin creates data, Manager allocates, Employee submits timesheet, Manager views health/AI.

Deliverable: end-to-end demo-ready PRM project.

## Daily Working Rhythm

- Start each day by reviewing the previous day's unfinished items.
- Keep commits or checkpoints small by feature area.
- Add tests for service rules before moving to the next module.
- Update this tracker at the end of each day with completed work, blockers, and next priorities.

## Progress Log

### 2026-06-02 - Day 1

- Created classic `ProjectResourceManagement.sln`.
- Created projects:
  - `src/Shared/ProjectResourceManagement.Shared.csproj`
  - `src/Server/ProjectResourceManagement.Server.csproj`
  - `src/Client/ProjectResourceManagement.Client.csproj`
  - `tests/Server.Tests/ProjectResourceManagement.Server.Tests.csproj`
- Added solution/project references.
- Removed template WeatherForecast API and added `/health`.
- Added server connection string configuration.
- Added README setup/run instructions.
- Added `.gitignore`.
- Verified:
  - `dotnet restore ProjectResourceManagement.sln`
  - `dotnet build ProjectResourceManagement.sln --no-restore`
  - `dotnet test ProjectResourceManagement.sln --no-build --no-restore`
- Note: NuGet restore for xUnit packages required external access to `api.nuget.org`.

### 2026-06-05 - V4 BRD Review and Day 2

- Updated active BRD source to `PRM_BRD_V4.md`.
- Captured V4 changes around manager assignment, team-scoped manager visibility, project update, and story-point tracking.
- Added EF Core/MySQL provider package through `Pomelo.EntityFrameworkCore.MySql`.
- Implemented shared enums:
  - User, employee, project, project health, milestone, allocation, timesheet, skill category, and proficiency statuses.
- Implemented initial auth DTOs.
- Implemented server entities:
  - User, Employee, Skill, EmployeeSkill, Project, Milestone, Allocation, Timesheet, TimesheetEntry, ActivityTag, SystemConfiguration.
- Implemented `ApplicationDbContext` relationships, indexes, enum conversions, precision settings, and seed data.
- Wired `ApplicationDbContext` into server startup.
- Added shared business-rule constants.
- Added initial repositories for users, employees, projects, allocations, and timesheets.
- Registered repositories in server DI.
- Verified:
  - `dotnet build ProjectResourceManagement.sln --no-restore`
  - `dotnet test ProjectResourceManagement.sln --no-build --no-restore`

### 2026-06-05 - Day 3 Authentication

- Added auth result contracts for service-level success/failure handling.
- Added `ChangePasswordResponse`.
- Implemented PBKDF2 password hashing with random salt and fixed-time verification.
- Updated first admin seed password hash for `Admin@1234`.
- Implemented `AuthService` login and password-change logic.
- Implemented `AuthController` endpoints:
  - `POST /api/auth/login`
  - `POST /api/auth/change-password`
- Registered auth services in dependency injection.
- Added EF Core InMemory package for server tests.
- Added auth tests for:
  - successful login
  - invalid password
  - inactive user blocked
  - successful password change
  - mismatched password confirmation
  - password hasher verification
- Added explanation document: `docs/Day-3-Code-Explanation.md`.
- Verified:
  - `dotnet build ProjectResourceManagement.sln --no-restore`
  - `dotnet test ProjectResourceManagement.sln --no-build --no-restore`
