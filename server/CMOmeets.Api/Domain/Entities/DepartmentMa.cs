using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class DepartmentMa
{
    public int Rid { get; set; }

    public int? MinistryId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public string? DepartmentNameHin { get; set; }

    public string? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual ICollection<MasDeptDesignation> MasDeptDesignations { get; set; } = new List<MasDeptDesignation>();

    public virtual MinistryMa? Ministry { get; set; }

    public virtual ICollection<TblOfficer> TblOfficers { get; set; } = new List<TblOfficer>();
}
