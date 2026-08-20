using System;

namespace CMOmeets.Domain.Entities;

// Junction listing the full set of departments an officer serves. TblOfficer.DeptId stays the
// officer's "home" department (it drives the dept-scoped designation); this table adds the extra
// departments used to scope a multi-department officer login. The home dept is included here too,
// so an officer's effective department set = the rows in this table.
public partial class TblOfficerDepartment
{
    public long Rid { get; set; }

    public int OfficerId { get; set; }

    public int DeptId { get; set; }

    public string Active { get; set; } = "Y";

    public virtual TblOfficer Officer { get; set; } = null!;

    public virtual DepartmentMa Dept { get; set; } = null!;
}
