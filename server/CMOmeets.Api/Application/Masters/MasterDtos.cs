namespace CMOmeets.Application.Masters;

public record MinistryDto(int Rid, string MinistryName, int DepartmentCount);
public record MinistrySaveDto(string MinistryName);

public record DepartmentDto(int Rid, int? MinistryId, string? MinistryName, string DepartmentName, string? DepartmentNameHin, bool Active);
public record DepartmentSaveDto(int? MinistryId, string DepartmentName, string? DepartmentNameHin, bool Active);

public record DesignationDto(int Rid, int DeptId, string DepartmentName, string DesigName, int SeqNo, bool Active);
public record DesignationSaveDto(int DeptId, string DesigName, int SeqNo, bool Active);

// Departments is the full set of departments the officer serves; Designations is the full set of
// designations they hold (one per department — each designation belongs to exactly one department).
// DeptId/DepartmentName and DesigId/DesigName echo the primary (first) department and its designation
// for backward-compatible ordering/scoping and single-designation displays.
public record OfficerDto(int Rid, int DeptId, string DepartmentName, int? DesigId, string? DesigName, string OfficerName, string OfficerMobile, string OfficerEmail, bool Active, List<LookupDto> Departments, List<LookupDto> Designations, List<OfficerDepartmentDesignationDto>? DepartmentDesignations);
// Force = the caller confirmed reassigning posts that are already held by other officers.
public record OfficerSaveDto(List<int> DesignationIds, string OfficerName, string OfficerMobile, string OfficerEmail, bool Active, List<int> DepartmentIds, bool Force = false);
// A post (designation) that is already held by another active officer, returned on a 409 so the UI
// can offer to remove that officer from it and reassign it.
public record DesignationConflictDto(int DesigId, string DesigName, int HolderOfficerId, string HolderOfficerName);

public record DistrictDto(string DCode, string DName, bool IsActive);
public record DistrictSaveDto(string DCode, string DName, bool IsActive);

public record LookupDto(int Id, string Name);
//created by Developer
public record OfficerDepartmentDesignationDto(
    int DepartmentId,
    string? DepartmentName,
    int DesignationId,
    string? DesignationName
);

