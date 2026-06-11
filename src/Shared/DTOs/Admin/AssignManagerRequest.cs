namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record AssignManagerRequest(
    int UserId,
    int ManagerUserId);
