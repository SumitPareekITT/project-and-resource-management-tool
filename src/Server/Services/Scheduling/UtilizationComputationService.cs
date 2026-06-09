using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Scheduling;

public sealed class UtilizationComputationService(
    EmployeeRepository employeeRepository,
    AllocationRepository allocationRepository)
{
    public async Task<int> SyncAllActiveEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await employeeRepository.ListActiveAsync(cancellationToken);
        foreach (var employee in employees)
        {
            ApplyUtilization(employee, await allocationRepository.ListActiveByEmployeeAsync(employee.Id, cancellationToken));
        }

        await employeeRepository.SaveChangesAsync(cancellationToken);
        return employees.Count;
    }

    public async Task SyncEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        var activeAllocations = await allocationRepository.ListActiveByEmployeeAsync(employee.Id, cancellationToken);
        ApplyUtilization(employee, activeAllocations);
    }

    internal static void ApplyUtilization(Employee employee, IReadOnlyList<Allocation> activeAllocations)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentUtilization = activeAllocations
            .Where(allocation => IsActiveOnDate(allocation, today))
            .Sum(allocation => allocation.UtilizationPercentage);

        employee.CurrentUtilizationPercent = currentUtilization;
        employee.Status = currentUtilization switch
        {
            0 => EmployeeStatus.Bench,
            BusinessRules.FullAllocationPercent => EmployeeStatus.Allocated,
            > BusinessRules.FullAllocationPercent => EmployeeStatus.Allocated,
            _ => EmployeeStatus.PartiallyAllocated
        };
    }

    private static bool IsActiveOnDate(Allocation allocation, DateOnly date)
    {
        return allocation.Status == AllocationStatus.Active
            && allocation.FromDate <= date
            && (allocation.ToDate is null || allocation.ToDate >= date);
    }
}
