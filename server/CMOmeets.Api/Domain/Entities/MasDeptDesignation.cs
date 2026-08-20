using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class MasDeptDesignation
{
    public int Rid { get; set; }

    public int DeptId { get; set; }

    public string DesigName { get; set; } = null!;

    public int SeqNo { get; set; }

    public string Active { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public virtual DepartmentMa Dept { get; set; } = null!;

    public virtual ICollection<TblOfficer> TblOfficers { get; set; } = new List<TblOfficer>();
}
