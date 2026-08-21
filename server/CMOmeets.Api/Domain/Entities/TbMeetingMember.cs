using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TbMeetingMember
{
    public long Rid { get; set; }
    public int MeetingRid { get; set; }

    public int MemberRid { get; set; }

    public DateTime? AddedAt { get; set; }
    public int DesignationId { get; set; }
    public int DepartmentId { get; set; }

    public virtual TbMeetingSchedule MeetingR { get; set; } = null!;

    public virtual TblOfficer MemberR { get; set; } = null!;
}
