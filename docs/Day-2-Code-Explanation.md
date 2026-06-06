# Day 2 Code Explanation

This file explains what was implemented on Day 2 and why it was needed for the PRM project.

Day 2 goal: create the shared domain vocabulary, database model, EF Core context, seed data, and repository foundation required by the V4 BRD.

## Why Day 2 Matters

Day 1 made the project buildable. Day 2 gives the application its actual business shape.

The BRD describes users, employees, managers, projects, allocations, timesheets, skills, activity tags, AI matching, and project health. Before we can build APIs or console screens, the server needs a clear domain model and database structure.

So Day 2 answers:

- What objects exist in the system?
- How are they related?
- What statuses and fixed choices are allowed?
- What data must be seeded initially?
- How will future services read/write the database cleanly?

## Shared Project

Path: `src/Shared`

The Shared project contains code used by both the server and client. This prevents the client and server from inventing different names for the same concepts.

### Enums

Enums were added under `src/Shared/Enums`.

They represent fixed values from the BRD:

- `UserRole`: Admin, Manager, Employee.
- `EmployeeStatus`: Bench, Allocated, PartiallyAllocated, Inactive.
- `ProjectStatus`: Planned, Active, OnHold, Completed, Cancelled.
- `ProjectHealthStatus`: OnTrack, Attention, AtRisk.
- `MilestoneStatus`: NotStarted, InProgress, Completed, Delayed, Blocked.
- `TimesheetStatus`: Draft, Submitted, Missed.
- `AllocationStatus`: Active, Ended.
- `SkillCategory`: Backend, Frontend, DevOps, QA, Other.
- `ProficiencyLevel`: Beginner, Intermediate, Advanced, Expert.

Why we did this:

- Avoids magic strings like `"ADMIN"` or `"BENCH"` spread throughout the code.
- Makes invalid values harder to use.
- Keeps client/server contracts consistent.
- Helps future validation and menu options stay aligned with the BRD.

### Auth DTOs

DTOs were added under `src/Shared/DTOs/Auth`.

- `LoginRequest`: username and password sent by the console client.
- `LoginResponse`: user identity, role, and whether password change is forced.
- `ChangePasswordRequest`: data required for first-login password change.

Why we did this:

- DTOs define the API contract between client and server.
- The console app should not send or receive database entities directly.
- Login is the first feature of Day 3, so these contracts are prepared now.

### Business Rules

Path: `src/Shared/Constants/BusinessRules.cs`

This file stores common rule constants:

- Default max weekly hours: `40`.
- Full allocation percentage: `100`.
- Minimum allocation percentage: `1`.
- Minimum password length.
- Default scheduler interval.

Why we did this:

- The BRD repeats rules like 40 max weekly hours and 100% max allocation.
- Constants make those rules easy to reuse and change.
- It avoids hidden numbers inside service logic later.

## Server Models

Path: `src/Server/Models`

These classes are database entities. They represent the main business objects in the PRM system.

## User

Represents login/account data.

Important fields:

- `Id`
- `FullName`
- `Email`
- `Username`
- `PasswordHash`
- `Role`
- `ForcePasswordChange`
- `IsActive`

Important relationships:

- A user may have one employee profile.
- A manager user can manage many employees.
- A manager user can own many projects.

Why we implemented it:

- The BRD says all access is role-based.
- Admin creates all accounts.
- First admin is seeded.
- Admin-created users must change password on first login.
- Deactivated users cannot log in.

## Employee

Represents a work profile for a person who can be allocated to projects or submit timesheets.

Important fields:

- `UserId`: links employee profile to login account.
- `ManagerId`: V4 requirement for direct-team ownership.
- `Department`
- `Designation`
- `Status`
- `CurrentUtilizationPercent`
- `IsActive`

Important relationships:

- Belongs to one `User`.
- May be assigned to one manager.
- Has many skills.
- Has many allocations.
- Has many timesheets.

Why we implemented it:

- The BRD separates user account from employee profile.
- V4 adds direct manager assignment.
- Manager screens must show only their assigned team.

## Skill and EmployeeSkill

`Skill` is the master skill list.

`EmployeeSkill` links employees to skills with proficiency.

Important fields:

- `Skill.Name`
- `Skill.Category`
- `EmployeeSkill.ProficiencyLevel`
- `EmployeeSkill.YearsOfExperience`
- `EmployeeSkill.LastUsedOn`

Why we implemented it:

- Admin manages employee skills.
- AI matching needs profile skills.
- Timesheet tags later improve evidence of real skill usage.

## Project

Represents a client/project managed by a manager.

Important fields:

- `Name`
- `ClientName`
- `Description`
- `StartDate`
- `EndDate`
- `Status`
- `HealthStatus`
- `ManagerId`
- `TotalStoryPoints`
- `CompletedStoryPoints`

Important relationships:

- Owned by one manager user.
- Has many milestones.
- Has many allocations.
- Has many timesheet entries.

Why we implemented it:

- Admin creates and updates projects.
- Manager sees "My Projects".
- V4 adds story-point tracking.
- Project health needs milestones, timesheets, allocations, and story-point progress.

## Milestone

Represents project milestones.

Important fields:

- `ProjectId`
- `Title`
- `DueDate`
- `Status`
- `StoryPoints`
- `CompletedStoryPoints`

Why we implemented it:

- Admin manages milestones.
- Project health depends on overdue/delayed milestones.
- V4 story-point progress can be tracked at project and milestone level.

## Allocation

Represents assigning an employee to a project for a date range and utilization percentage.

Important fields:

- `EmployeeId`
- `ProjectId`
- `CreatedByManagerId`
- `UtilizationPercentage`
- `FromDate`
- `ToDate`
- `Status`

Why we implemented it:

- Manager allocates employees.
- Allocation must prevent over-allocation beyond 100%.
- V4 says managers can allocate only direct-team employees.
- Only project-owning managers can end allocations on their projects.

## Timesheet and TimesheetEntry

`Timesheet` represents one employee's weekly submission.

`TimesheetEntry` represents hours logged against a specific project.

Important fields:

- `Timesheet.EmployeeId`
- `Timesheet.WeekStartDate`
- `Timesheet.TotalHours`
- `Timesheet.Status`
- `TimesheetEntry.ProjectId`
- `TimesheetEntry.HoursWorked`

Why we implemented it:

- Employees submit weekly timesheets.
- The server must prevent duplicate weekly submissions.
- Hours must be validated against allocations.
- Manager can view team timesheets.
- Project health and AI risk summaries use logged hours.

## ActivityTag

Represents the type of work done in a timesheet entry.

Examples:

- Backend API Development
- Microservices / Architecture
- Database Design & Queries
- WebSocket / Real-time Features
- Testing & QA

Why we implemented it:

- BRD says activity tags become real evidence of skill usage.
- AI matching can use recent activity tags, not only static skill profiles.

## SystemConfiguration

Stores configurable settings.

Examples:

- Max weekly hours.
- Scheduler interval.
- LLM provider.
- LLM API key placeholder.

Why we implemented it:

- Admin must configure system settings.
- Business rules like max weekly hours should not be hardcoded forever.
- AI provider settings are part of the BRD.

## ApplicationDbContext

Path: `src/Server/Data/ApplicationDbContext.cs`

This is the EF Core database context. It maps our C# entity classes to database tables.

It includes `DbSet` properties for:

- Users
- Employees
- Skills
- EmployeeSkills
- Projects
- Milestones
- Allocations
- Timesheets
- TimesheetEntries
- ActivityTags
- SystemConfigurations

Why we implemented it:

- EF Core needs a context to query and save data.
- It centralizes table relationships and constraints.
- It becomes the foundation for repositories and services.

## Important EF Configuration

### Unique User Fields

`Username` and `Email` are unique.

Why:

- Login usernames must not duplicate.
- Email is also expected to identify one user.

### User to Employee

A user can have zero or one employee profile.

Why:

- Admin users do not need employee profiles.
- Manager and Employee users may have work profiles.

### Employee to Manager

Employee has `ManagerId`.

Why:

- V4 requires direct manager assignment.
- Manager dashboards and allocations are scoped to direct team.

### Project to Manager

Project has `ManagerId`.

Why:

- Managers own projects.
- Only owning managers should allocate/end allocation for that project.

### EmployeeSkill Composite Key

`EmployeeSkill` uses `{ EmployeeId, SkillId }` as a composite key.

Why:

- One employee should not have the same skill duplicated.

### Timesheet Unique Constraint

Timesheets are unique by `{ EmployeeId, WeekStartDate }`.

Why:

- BRD says the same weekly timesheet cannot be submitted twice.

### TimesheetEntry to ActivityTag

Many-to-many relationship.

Why:

- A timesheet entry can have multiple tags.
- A tag can be used by many entries.

### Enum Conversion

Enums are stored as strings.

Why:

- Database rows are more readable.
- Values like `Admin`, `Bench`, `AtRisk` are clearer than numbers.

## SeedData

Path: `src/Server/Data/SeedData.cs`

Seed data gives the database initial values.

Seeded:

- First admin user.
- Default activity tags.
- System configuration records.

Why:

- The BRD says the first admin must be bootstrapped.
- Activity tags should be available for timesheet screens.
- System settings need defaults before Admin edits them.

Note:

The seeded admin currently has placeholder password hash:

```text
CHANGE_ME_WITH_PASSWORD_HASHER
```

This will be replaced during Day 3 when password hashing is implemented.

## Repositories

Path: `src/Server/Data/Repositories`

Repositories provide a clean way for services to access database data.

Implemented repositories:

- `UserRepository`
- `EmployeeRepository`
- `ProjectRepository`
- `AllocationRepository`
- `TimesheetRepository`

Why we implemented them:

- Services should focus on business rules, not EF query details.
- This demonstrates the Repository pattern required by the BRD.
- Later unit tests become easier because service dependencies are clearer.

## UserRepository

Main methods:

- Get user by ID.
- Get user by username.
- List users.
- Add user.
- Save changes.

Used later by:

- Login.
- User management.
- Password reset.
- Deactivation.

## EmployeeRepository

Main methods:

- Get employee by ID.
- Get employee by user ID.
- List all employees.
- List employees by manager ID.
- Add employee.
- Save changes.

Used later by:

- Admin employee management.
- Assign Manager.
- Manager dashboard.
- Team-scoped allocation.

## ProjectRepository

Main methods:

- Get project by ID.
- List all projects.
- List projects by manager ID.
- Add project.
- Save changes.

Used later by:

- Admin project management.
- Manager My Projects.
- Allocation ownership validation.
- Project health.

## AllocationRepository

Main methods:

- Get allocation by ID.
- List active allocations by employee.
- List active allocations by project.
- Add allocation.
- Save changes.

Used later by:

- Allocation validation.
- Utilization calculation.
- Employee allocation view.
- Project allocation matrix.

## TimesheetRepository

Main methods:

- Get timesheet by employee/week.
- List timesheets by employee.
- List timesheets for a manager's team.
- Add timesheet.
- Save changes.

Used later by:

- Duplicate prevention.
- Employee timesheet history.
- Manager read-only timesheet view.
- Project health and AI risk summaries.

## Program.cs Changes

Path: `src/Server/Program.cs`

We registered:

- `ApplicationDbContext`
- MySQL provider
- repositories
- controllers

Why:

- The server needs EF Core configured at startup.
- Repositories need to be available through dependency injection.
- Controllers and services later can request these dependencies cleanly.

## How Day 2 Supports V4 BRD

V4 requirement: Assign Manager.

Implemented foundation:

- `Employee.ManagerId`
- `User.ManagedEmployees`
- `EmployeeRepository.ListByManagerIdAsync`

V4 requirement: Managers see only direct team.

Implemented foundation:

- Team-scoped employee queries.
- Team-scoped timesheet queries.
- Allocation repository ready for service validation.

V4 requirement: Project story points.

Implemented foundation:

- `Project.TotalStoryPoints`
- `Project.CompletedStoryPoints`
- `Milestone.StoryPoints`
- `Milestone.CompletedStoryPoints`

V4 requirement: Project health.

Implemented foundation:

- `Project.HealthStatus`
- Milestones with due dates/status.
- Allocations and timesheets connected to projects.

V4 requirement: AI skill matcher.

Implemented foundation:

- Employee skills.
- Activity tags.
- Allocations for availability.
- Timesheets for recent work evidence.

## What Was Not Implemented Yet

Day 2 intentionally did not implement business workflows.

Not done yet:

- Password hashing.
- Login API.
- Auth tokens/session strategy.
- Admin APIs.
- Manager APIs.
- Employee timesheet APIs.
- Scheduler jobs.
- AI provider calls.
- Real database migrations.

These come in later days.

## Day 3 Starts From Here

Day 3 should build on this foundation:

1. Add password hashing.
2. Replace seeded admin placeholder password hash.
3. Implement `AuthService`.
4. Implement login and forced password change APIs.
5. Add tests for login success/failure, forced password change, and inactive user blocking.

