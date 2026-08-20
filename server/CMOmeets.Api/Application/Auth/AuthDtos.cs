namespace CMOmeets.Application.Auth;

public record LoginRequest(string Username, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AuthUserDto(
    string Username,
    string DisplayName,
    string? Designation,
    string Role,
    string? LocationName,
    int? DepartmentId,
    string? DepartmentName,
    int? OfficerId,
    string? OfficerName);

public record AuthResult(string Token, DateTime ExpiresAt, AuthUserDto User);

// A department an officer login serves — drives the title-bar department switcher.
public record OfficerDeptDto(int Id, string Name);
