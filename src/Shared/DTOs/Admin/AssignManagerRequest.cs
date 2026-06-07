namespace ProjectResourceManagement.Shared.DTOs.Admin;

public sealed record AssignManagerRequest(int EmployeeUserId, int ManagerUserId);
