using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TbMeetingAgenda
{
    public long Rid { get; set; }

    public int MeetingRid { get; set; }

    public string MeetingAgenda { get; set; } = null!;

    public string? AgendaMembers { get; set; }

    public string? MemberRids { get; set; }

    public DateOnly? AgendaDueDt { get; set; }

    public string DistrictName { get; set; } = null!;

    public string AgendaStatus { get; set; } = null!;

    public string? Active { get; set; }

    // Admin-maintained follow-up: whether the concerned officer has been called about this point,
    // and the admin's note about that call. Only meaningful while IsOfficerCalled is true.
    public bool IsOfficerCalled { get; set; }

    public string? OfficerRemark { get; set; }

    public DateTime? AddedAt { get; set; }

    public string? AddedBy { get; set; }

    public virtual TbMeetingSchedule MeetingR { get; set; } = null!;

    public virtual ICollection<TbRemarksOnAgenda> TbRemarksOnAgenda { get; set; } = new List<TbRemarksOnAgenda>();
}
