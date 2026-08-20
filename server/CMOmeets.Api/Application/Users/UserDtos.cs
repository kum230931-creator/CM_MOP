namespace CMOmeets.Application.Users;

public record UserListDto(
    string Id, string Username, string DisplayName, string Role,
    int? DepartmentId, string? DepartmentName, int? OfficerId, string? OfficerName, bool IsActive,
    // Populated for a 'cmo_officer' login: the directly-chosen department set (ids + joined names).
    List<int>? DepartmentIds = null, string? DepartmentNames = null);

public record CreateUserDto(
    string Username, string Password, string DisplayName, string Role, int? DepartmentId, int? OfficerId,
    List<int>? DepartmentIds = null);

public record UpdateUserDto(string DisplayName, int? DepartmentId, int? OfficerId, bool IsActive,
    List<int>? DepartmentIds = null);

public record ResetPasswordDto(string NewPassword);
