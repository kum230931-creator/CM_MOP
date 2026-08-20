using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class MinistryMa
{
    public int Rid { get; set; }

    public string MinistryName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DepartmentMa> DepartmentMas { get; set; } = new List<DepartmentMa>();
}
