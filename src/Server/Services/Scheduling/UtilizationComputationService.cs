using ProjectResourceManagement.Server.Data.Repositories;
using ProjectResourceManagement.Server.Models;
using ProjectResourceManagement.Shared.Constants;
using ProjectResourceManagement.Shared.Enums;

namespace ProjectResourceManagement.Server.Services.Scheduling;

public sealed class UtilizationComputationService(
    UserProfileRepository userProfileRepository,
    AllocationRepository allocationRepository)
{
    public async Task<int> SyncAllActiveProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await userProfileRepository.ListActiveAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            ApplyUtilization(profile, await allocationRepository.ListActiveByUserIdAsync(profile.UserId, cancellationToken));
        }

        await userProfileRepository.SaveChangesAsync(cancellationToken);
        return profiles.Count;
    }

    public async Task SyncUserProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        var activeAllocations = await allocationRepository.ListActiveByUserIdAsync(profile.UserId, cancellationToken);
        ApplyUtilization(profile, activeAllocations);
    }

    internal static void ApplyUtilization(UserProfile profile, IReadOnlyList<Allocation> activeAllocations)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentUtilization = activeAllocations
            .Where(allocation => IsActiveOnDate(allocation, today))
            .Sum(allocation => allocation.UtilizationPercentage);

        profile.CurrentUtilizationPercent = currentUtilization;
        profile.ResourceStatus = currentUtilization switch
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
