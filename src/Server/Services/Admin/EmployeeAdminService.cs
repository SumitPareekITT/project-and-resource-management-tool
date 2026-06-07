using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.DTOs.Admin;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Admin;

public sealed class EmployeeAdminService(
    EmployeeRepository employeeRepository,
    UserRepository userRepository,
    SkillRepository skillRepository,
    AllocationRepository allocationRepository)
{
    public async Task<AdminResult<IReadOnlyList<EmployeeSummaryDto>>> ListEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await employeeRepository.ListAsync(cancellationToken);
        var mapped = employees.Select(MapEmployee).ToList();
        return AdminResult<IReadOnlyList<EmployeeSummaryDto>>.Success(mapped);
    }

    public async Task<AdminResult<EmployeeSummaryDto>> CreateEmployeeAsync(
        CreateEmployeeProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateProfileFields(request.FullName, request.Email, request.Department, request.Designation);
        if (validationError is not null)
        {
            return validationError;
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "User account was not found.");
        }

        if (user.Role is not UserRole.Employee and not UserRole.Manager)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Employee profile can be linked only to Employee or Manager users.");
        }

        var existingEmployee = await employeeRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingEmployee is not null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.Conflict, "Employee profile already exists for this user.");
        }

        var normalizedEmail = request.Email.Trim();
        var userByEmail = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (userByEmail is not null && userByEmail.Id != request.UserId)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.Conflict, "Email is already used by another user account.");
        }

        var employeeByEmail = await employeeRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (employeeByEmail is not null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.Conflict, "Email is already used by another employee profile.");
        }

        var managerValidation = await ValidateManagerAsync(request.UserId, request.ManagerId, cancellationToken);
        if (managerValidation is not null)
        {
            return managerValidation;
        }

        var employee = new Employee
        {
            UserId = request.UserId,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Department = request.Department.Trim(),
            Designation = request.Designation.Trim(),
            ManagerId = request.ManagerId,
            Status = EmployeeStatus.Bench,
            IsActive = true
        };

        await employeeRepository.AddAsync(employee, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);

        var createdEmployee = await employeeRepository.GetByIdAsync(employee.Id, cancellationToken);
        return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(createdEmployee!));
    }

    public async Task<AdminResult<EmployeeSummaryDto>> UpdateEmployeeAsync(
        int employeeId,
        UpdateEmployeeProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "Employee was not found.");
        }

        var validationError = ValidateProfileFields(request.FullName, request.Email, request.Department, request.Designation);
        if (validationError is not null)
        {
            return validationError;
        }

        var managerValidation = await ValidateManagerAsync(employee.UserId, request.ManagerId, cancellationToken);
        if (managerValidation is not null)
        {
            return managerValidation;
        }

        var normalizedEmail = request.Email.Trim();
        var userByEmail = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (userByEmail is not null && userByEmail.Id != employee.UserId)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.Conflict, "Email is already used by another user account.");
        }

        var employeeByEmail = await employeeRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (employeeByEmail is not null && employeeByEmail.Id != employee.Id)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.Conflict, "Email is already used by another employee profile.");
        }

        employee.FullName = request.FullName.Trim();
        employee.Email = normalizedEmail;
        employee.Department = request.Department.Trim();
        employee.Designation = request.Designation.Trim();
        employee.ManagerId = request.ManagerId;

        await employeeRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(employee));
    }

    public async Task<AdminResult<EmployeeSummaryDto>> AssignManagerAsync(
        AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmployeeUserId == request.ManagerUserId)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Employee user and manager user cannot be the same.");
        }

        var employeeUser = await userRepository.GetByIdAsync(request.EmployeeUserId, cancellationToken);
        if (employeeUser is null || employeeUser.Role != UserRole.Employee)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Employee user must exist with Employee role.");
        }

        var managerUser = await userRepository.GetByIdAsync(request.ManagerUserId, cancellationToken);
        if (managerUser is null || managerUser.Role != UserRole.Manager || !managerUser.IsActive)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Manager user must exist with active Manager role.");
        }

        var employee = await employeeRepository.GetByUserIdAsync(request.EmployeeUserId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "Employee profile was not found.");
        }

        employee.ManagerId = request.ManagerUserId;
        await employeeRepository.SaveChangesAsync(cancellationToken);

        var updatedEmployee = await employeeRepository.GetByIdAsync(employee.Id, cancellationToken);
        return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(updatedEmployee!));
    }

    public async Task<AdminResult<EmployeeSummaryDto>> DeactivateEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "Employee was not found.");
        }

        if (!employee.IsActive)
        {
            return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(employee), "Employee is already inactive.");
        }

        var utcNow = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        employee.IsActive = false;
        employee.Status = EmployeeStatus.Inactive;
        employee.CurrentUtilizationPercent = 0;
        employee.DeactivatedAtUtc = utcNow;

        employee.User.IsActive = false;
        employee.User.DeactivatedAtUtc = utcNow;

        var activeAllocations = await allocationRepository.ListActiveByEmployeeAsync(employee.Id, cancellationToken);
        foreach (var allocation in activeAllocations)
        {
            allocation.Status = AllocationStatus.Ended;
            if (allocation.ToDate is null || allocation.ToDate > today)
            {
                allocation.ToDate = today;
            }
        }

        await employeeRepository.SaveChangesAsync(cancellationToken);
        return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(employee), "Employee deactivated successfully.");
    }

    public async Task<AdminResult<EmployeeSummaryDto>> UpsertEmployeeSkillAsync(
        int employeeId,
        UpsertEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.YearsOfExperience is < 0)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Years of experience cannot be negative.");
        }

        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "Employee was not found.");
        }

        var skill = await skillRepository.GetByIdAsync(request.SkillId, cancellationToken);
        if (skill is null || !skill.IsActive)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Skill must exist and be active.");
        }

        var existing = employee.Skills.FirstOrDefault(skillLink => skillLink.SkillId == request.SkillId);
        if (existing is null)
        {
            employee.Skills.Add(new EmployeeSkill
            {
                EmployeeId = employee.Id,
                SkillId = request.SkillId,
                ProficiencyLevel = request.ProficiencyLevel,
                YearsOfExperience = request.YearsOfExperience,
                LastUsedOn = request.LastUsedOn
            });
        }
        else
        {
            existing.ProficiencyLevel = request.ProficiencyLevel;
            existing.YearsOfExperience = request.YearsOfExperience;
            existing.LastUsedOn = request.LastUsedOn;
        }

        await employeeRepository.SaveChangesAsync(cancellationToken);
        var updatedEmployee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(updatedEmployee!));
    }

    public async Task<AdminResult<EmployeeSummaryDto>> RemoveEmployeeSkillAsync(
        int employeeId,
        int skillId,
        CancellationToken cancellationToken = default)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "Employee was not found.");
        }

        var existing = employee.Skills.FirstOrDefault(skillLink => skillLink.SkillId == skillId);
        if (existing is null)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.NotFound, "Employee skill mapping was not found.");
        }

        employee.Skills.Remove(existing);
        await employeeRepository.SaveChangesAsync(cancellationToken);

        var updatedEmployee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        return AdminResult<EmployeeSummaryDto>.Success(MapEmployee(updatedEmployee!));
    }

    private static AdminResult<EmployeeSummaryDto>? ValidateProfileFields(string fullName, string email, string department, string designation)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Department is required.");
        }

        if (string.IsNullOrWhiteSpace(designation))
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Designation is required.");
        }

        return null;
    }

    private async Task<AdminResult<EmployeeSummaryDto>?> ValidateManagerAsync(
        int employeeUserId,
        int? managerId,
        CancellationToken cancellationToken)
    {
        if (managerId is null)
        {
            return null;
        }

        if (employeeUserId == managerId.Value)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Employee cannot report to self.");
        }

        var managerUser = await userRepository.GetByIdAsync(managerId.Value, cancellationToken);
        if (managerUser is null || managerUser.Role != UserRole.Manager || !managerUser.IsActive)
        {
            return AdminResult<EmployeeSummaryDto>.Fail(AdminResultCode.ValidationError, "Manager must be an active user with Manager role.");
        }

        return null;
    }

    private static EmployeeSummaryDto MapEmployee(Employee employee)
    {
        var skills = employee.Skills
            .OrderBy(skill => skill.Skill.Name)
            .Select(skill => new EmployeeSkillDto(
                skill.SkillId,
                skill.Skill.Name,
                skill.Skill.Category,
                skill.ProficiencyLevel,
                skill.YearsOfExperience,
                skill.LastUsedOn))
            .ToList();

        return new EmployeeSummaryDto(
            employee.Id,
            employee.UserId,
            employee.FullName,
            employee.Email,
            employee.Department,
            employee.Designation,
            employee.Status,
            employee.CurrentUtilizationPercent,
            employee.IsActive,
            employee.ManagerId,
            employee.Manager?.FullName,
            skills);
    }
}
