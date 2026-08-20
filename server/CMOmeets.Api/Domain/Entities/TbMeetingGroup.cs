using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TbMeetingGroup
{
    public long Rid { get; set; }

    public string GroupName { get; set; } = null!;

    public string Active { get; set; } = null!;

    public DateTime AddedAt { get; set; }

    public string AddedBy { get; set; } = null!;

    public virtual ICollection<TbMeetingMappedGroup> TbMeetingMappedGroups { get; set; } = new List<TbMeetingMappedGroup>();
}
