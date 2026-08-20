using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class DesignationMa
{
    public int Rid { get; set; }

    public string DesignationName { get; set; } = null!;

    public string OfficerName { get; set; } = null!;

    public string? MobileNo { get; set; }

    public int? SeqNo { get; set; }

    public string? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }
}
