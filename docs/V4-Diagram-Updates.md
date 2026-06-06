# V4 Diagram Updates

These PlantUML diagrams update the original design document for the V4 BRD changes:

- Employee manager assignment.
- Manager-only direct-team visibility.
- Project story-point tracking.
- Explicit project detail update flow.
- Project health using milestones, story points, allocations, and timesheets.

## 1. Admin Use Case - Updated

```plantuml
@startuml
left to right direction
actor Admin

rectangle "PRM Tool - Admin Module" {
  usecase "Manage Users" as UC_Users
  usecase "Create User Account" as UC_CreateUser
  usecase "Reset Password" as UC_ResetPassword
  usecase "Deactivate User" as UC_DeactivateUser

  usecase "View / Update Employees" as UC_Employees
  usecase "Deactivate Employee" as UC_DeactivateEmployee
  usecase "Manage Employee Skills" as UC_Skills
  usecase "Assign Manager" as UC_AssignManager

  usecase "Manage Projects" as UC_Projects
  usecase "Create Project" as UC_CreateProject
  usecase "Update Project Details" as UC_UpdateProject
  usecase "Manage Milestones" as UC_Milestones

  usecase "View All Allocations" as UC_Allocations
  usecase "System Configuration" as UC_Config
}

Admin --> UC_Users
Admin --> UC_Employees
Admin --> UC_Projects
Admin --> UC_Allocations
Admin --> UC_Config

UC_Users .> UC_CreateUser : includes
UC_Users .> UC_ResetPassword : includes
UC_Users .> UC_DeactivateUser : includes

UC_Employees .> UC_DeactivateEmployee : includes
UC_Employees .> UC_Skills : includes
UC_Employees .> UC_AssignManager : includes

UC_Projects .> UC_CreateProject : includes
UC_Projects .> UC_UpdateProject : includes
UC_Projects .> UC_Milestones : includes
@enduml
```

## 2. Manager Use Case - Updated Team Scope

```plantuml
@startuml
left to right direction
actor Manager

rectangle "PRM Tool - Manager Module" {
  usecase "View Resource Dashboard" as UC_Dashboard
  usecase "Search Direct Team Only" as UC_TeamSearch
  usecase "Allocate Team Member" as UC_Allocate
  usecase "End Allocation\n(Owned Projects Only)" as UC_EndAllocation
  usecase "View My Projects" as UC_MyProjects
  usecase "View Team Timesheets" as UC_Timesheets
  usecase "AI Skill Matcher\n(Direct Team Only)" as UC_AiMatch
  usecase "AI Risk Summary" as UC_AiRisk
}

Manager --> UC_Dashboard
Manager --> UC_Allocate
Manager --> UC_MyProjects
Manager --> UC_Timesheets
Manager --> UC_AiMatch
Manager --> UC_AiRisk

UC_Dashboard .> UC_TeamSearch : filters by manager_id
UC_Allocate .> UC_TeamSearch : filters by manager_id
UC_Allocate .> UC_EndAllocation : optional
UC_AiMatch .> UC_TeamSearch : filters candidates first
UC_MyProjects .> UC_AiRisk : can request summary
@enduml
```

## 3. Assign Manager Sequence - New

```plantuml
@startuml
actor Admin
participant "Console Client" as Client
participant "EmployeeController" as Controller
participant "EmployeeService" as Service
participant "EmployeeRepository" as Employees
participant "UserRepository" as Users
database "PRM DB" as DB

Admin -> Client : Enter Employee User ID and Manager User ID
Client -> Controller : PUT /api/employees/assign-manager
Controller -> Service : AssignManager(employeeUserId, managerUserId)
Service -> Users : Get employee user
Users -> DB : Query user where role = EMPLOYEE
DB --> Users : Employee user
Service -> Users : Get manager user
Users -> DB : Query user where role = MANAGER
DB --> Users : Manager user
Service -> Employees : GetByUserId(employeeUserId)
Employees -> DB : Query employee profile
DB --> Employees : Employee
Service -> Employees : Set ManagerId = managerUserId
Employees -> DB : Save changes
Service --> Controller : Success
Controller --> Client : 200 OK
Client --> Admin : Manager assigned
@enduml
```

## 4. Resource Allocation Sequence - Updated Team Scope

```plantuml
@startuml
actor Manager
participant "Console Client" as Client
participant "AllocationController" as Controller
participant "AllocationService" as Service
participant "EmployeeRepository" as Employees
participant "ProjectRepository" as Projects
participant "AllocationRepository" as Allocations
database "PRM DB" as DB

Manager -> Client : Select project and employee
Client -> Controller : POST /api/allocations
Controller -> Service : Allocate(managerId, projectId, employeeId, percent, dates)
Service -> Projects : GetById(projectId)
Projects -> DB : Load project
DB --> Projects : Project

alt Project not owned by manager
  Service --> Controller : Reject
  Controller --> Client : 403 Forbidden
else Project owned by manager
  Service -> Employees : GetById(employeeId)
  Employees -> DB : Load employee
  DB --> Employees : Employee

  alt Employee not in manager direct team
    Service --> Controller : Reject
    Controller --> Client : 403 Forbidden
  else Employee in direct team
    Service -> Allocations : List active allocations
    Allocations -> DB : Sum overlapping utilization
    DB --> Allocations : Active allocation data
    Service -> Service : Validate total <= 100%
    Service -> Allocations : Add allocation
    Allocations -> DB : Save
    Controller --> Client : 201 Created
  end
end
@enduml
```

## 5. Project Update / Story Points Sequence - New

```plantuml
@startuml
actor Admin
participant "Console Client" as Client
participant "ProjectController" as Controller
participant "ProjectService" as Service
participant "ProjectRepository" as Projects
participant "UserRepository" as Users
database "PRM DB" as DB

Admin -> Client : Edit project details
Client -> Controller : PUT /api/projects/{id}
Controller -> Service : UpdateProject(command)
Service -> Projects : GetById(projectId)
Projects -> DB : Load project
DB --> Projects : Project
Service -> Users : Validate manager role
Users -> DB : Query manager user
DB --> Users : Manager
Service -> Service : Validate dates and story points
Service -> Projects : Update name, dates, status, manager, total SP
Projects -> DB : Save changes
Service --> Controller : Updated project
Controller --> Client : 200 OK
Client --> Admin : Project updated
@enduml
```

## 6. Project Health Sequence - Updated Story Points

```plantuml
@startuml
participant "ProjectHealthJob" as Job
participant "ProjectRepository" as Projects
participant "TimesheetRepository" as Timesheets
participant "AllocationRepository" as Allocations
participant "AiService" as AI
database "PRM DB" as DB

Job -> Projects : Load active projects with milestones
Projects -> DB : Query projects, milestones, SP done/total
DB --> Projects : Project facts

loop Each active project
  Job -> Allocations : Load active project allocations
  Allocations -> DB : Query allocation percentages
  DB --> Allocations : Allocation facts
  Job -> Timesheets : Load recent project timesheets
  Timesheets -> DB : Query logged hours and activity tags
  DB --> Timesheets : Timesheet facts
  Job -> Job : Evaluate overdue milestones, SP progress, logged effort
  Job -> Projects : Update HealthStatus
  Projects -> DB : Save health state
end

opt Manager requests AI risk summary
  Job -> AI : Send factual project summary only
  AI --> Job : Plain-English risk paragraph
end
@enduml
```

## 7. Data Model - Updated V4 Core

```plantuml
@startuml
hide circle
skinparam classAttributeIconSize 0

class User {
  Id
  FullName
  Email
  Username
  PasswordHash
  Role
  ForcePasswordChange
  IsActive
}

class Employee {
  Id
  UserId
  ManagerId
  Department
  Designation
  Status
  CurrentUtilizationPercent
}

class Project {
  Id
  Name
  ManagerId
  StartDate
  EndDate
  Status
  HealthStatus
  TotalStoryPoints
  CompletedStoryPoints
}

class Milestone {
  Id
  ProjectId
  Title
  DueDate
  Status
  StoryPoints
  CompletedStoryPoints
}

class Allocation {
  Id
  EmployeeId
  ProjectId
  CreatedByManagerId
  UtilizationPercentage
  FromDate
  ToDate
  Status
}

class Timesheet {
  Id
  EmployeeId
  WeekStartDate
  TotalHours
  Status
}

class TimesheetEntry {
  Id
  TimesheetId
  ProjectId
  HoursWorked
}

class Skill
class EmployeeSkill
class ActivityTag
class SystemConfiguration

User "1" -- "0..1" Employee : profile
User "1" -- "0..*" Employee : manages
User "1" -- "0..*" Project : owns
Employee "1" -- "0..*" EmployeeSkill
Skill "1" -- "0..*" EmployeeSkill
Project "1" -- "0..*" Milestone
Employee "1" -- "0..*" Allocation
Project "1" -- "0..*" Allocation
Employee "1" -- "0..*" Timesheet
Timesheet "1" -- "1..*" TimesheetEntry
Project "1" -- "0..*" TimesheetEntry
TimesheetEntry "0..*" -- "0..*" ActivityTag
@enduml
```

