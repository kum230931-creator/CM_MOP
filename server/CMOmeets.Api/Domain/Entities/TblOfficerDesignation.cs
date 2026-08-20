using System;

namespace CMOmeets.Domain.Entities;

// Junction listing the designations an officer holds — one per department they serve (each
// MAS_DeptDesignation belongs to exactly one department, so a designation implies its department).
// Lets a multi-department officer carry the right designation for each department (e.g. the one to
// show when they post an ATR on that department's action point). TblOfficer.DesigId stays the
// officer's primary designation; this table is the full set (and includes that primary).
public partial class TblOfficerDesignation
{
    public long Rid { get; set; }

    public int OfficerId { get; set; }

    public int DesigId { get; set; }

    public string Active { get; set; } = "Y";

    public virtual TblOfficer Officer { get; set; } = null!;

    public virtual MasDeptDesignation Desig { get; set; } = null!;
}
