using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TblOfficer
{
    public int Rid { get; set; }

    public int DeptId { get; set; }

    // Primary designation (post). Nullable: an officer can be left with no post when their only post
    // is reassigned to another officer (posts are unique to one officer).
    public int? DesigId { get; set; }

    public string OfficerName { get; set; } = null!;

    public string OfficerMobile { get; set; } = null!;

    public string OfficerEmail { get; set; } = null!;

    public string Active { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual DepartmentMa Dept { get; set; } = null!;

    public virtual MasDeptDesignation? Desig { get; set; }

    public virtual ICollection<TbMeetingMember> TbMeetingMembers { get; set; } = new List<TbMeetingMember>();

    // The full set of departments this officer serves (includes the home DeptId). Drives the
    // scope of a multi-department officer login.
    public virtual ICollection<TblOfficerDepartment> OfficerDepartments { get; set; } = new List<TblOfficerDepartment>();

    // The designations this officer holds — one per department they serve (includes the primary
    // DesigId). Lets the right designation be resolved for a given department.
    public virtual ICollection<TblOfficerDesignation> OfficerDesignations { get; set; } = new List<TblOfficerDesignation>();
}
