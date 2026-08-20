using Microsoft.AspNetCore.Identity;

namespace CMOmeets.Domain.Identity;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? LocationCode { get; set; }
    public string? LocationName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Set for 'dept' role logins — ties the account to a departmentMas.RID so it
    // only sees/acts on meetings that include an officer from this department.
    public int? DepartmentId { get; set; }

    // Set for 'officer' role logins — ties the account to a single tbl_Officers.RID so it
    // only sees/acts on action points where this officer is a responsible member.
    public int? OfficerId { get; set; }

    // Set for 'cmo_officer' role logins — a CSV of departmentMas.RID values. Unlike 'officer'
    // (whose departments come from an officer record), a CMO officer's departments are chosen
    // directly; it sees/acts on the union of officers across these departments, with no post.
    public string? DepartmentIds { get; set; }
}
