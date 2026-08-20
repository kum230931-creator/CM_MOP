using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TbMeetingSchedule
{
    public int Rid { get; set; }

    public DateTime MeetingDate { get; set; }

    public string MeetingPlace { get; set; } = null!;

    public string? MeetingSubject { get; set; }

    public string? MeetingDocument { get; set; }

    public string Active { get; set; } = null!;

    public DateTime? AddedAt { get; set; }

    public string? AddedBy { get; set; }

    public virtual ICollection<TbMeetingAgenda> TbMeetingAgenda { get; set; } = new List<TbMeetingAgenda>();

    public virtual ICollection<TbMeetingMappedGroup> TbMeetingMappedGroups { get; set; } = new List<TbMeetingMappedGroup>();

    public virtual ICollection<TbMeetingMember> TbMeetingMembers { get; set; } = new List<TbMeetingMember>();

    public virtual ICollection<TbRemarksOnAgenda> TbRemarksOnAgenda { get; set; } = new List<TbRemarksOnAgenda>();
}
