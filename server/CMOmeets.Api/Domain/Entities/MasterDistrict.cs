using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class MasterDistrict
{
    public string DName { get; set; } = null!;

    public string DCode { get; set; } = null!;

    public string IsActive { get; set; } = null!;
}
