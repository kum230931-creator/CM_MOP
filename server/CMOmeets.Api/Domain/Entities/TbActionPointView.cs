using System;

namespace CMOmeets.Domain.Entities;

// Log of a concerned department opening (expanding) an action point on the action-points page.
// One row per (action point, department); first/last opened times are tracked for the admin report.
public partial class TbActionPointView
{
    public long Rid { get; set; }

    public long AgendaRid { get; set; }

    public int DeptId { get; set; }

    public DateTime FirstViewedAt { get; set; }

    public DateTime LastViewedAt { get; set; }

    public string ViewedBy { get; set; } = null!;
}
