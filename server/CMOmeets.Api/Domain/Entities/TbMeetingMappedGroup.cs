using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TbMeetingMappedGroup
{
    public long Rid { get; set; }

    public long GroupRid { get; set; }

    public int MeetingRid { get; set; }

    public string Active { get; set; } = null!;

    public DateTime AddedAt { get; set; }

    public string AddedBy { get; set; } = null!;

    public virtual TbMeetingGroup GroupR { get; set; } = null!;

    public virtual TbMeetingSchedule MeetingR { get; set; } = null!;
}
