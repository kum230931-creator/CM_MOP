using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class TbRemarksOnAgenda
{
    public long Rid { get; set; }

    public int MeetingRid { get; set; }

    public long AgendaRid { get; set; }

    public DateOnly? AgendaDueDate { get; set; }

    public int MemberRid { get; set; }

    public string MemberName { get; set; } = null!;

    public string AgendaRemarks { get; set; } = null!;

    public DateOnly? RemarksDate { get; set; }

    public string? RemarkStatus { get; set; }

    public string? AtrDoc { get; set; }

    // 0-100 completion of the action point. 100 when the ATR is Completed; admin
    // explanation requests knock it down by 10 each time.
    public int? ProgressPercentage { get; set; }

    public DateTime? AddedAt { get; set; }

    public string? AddedBy { get; set; }

    // Admin <-> department explanation exchange about this ATR.
    public string? ExplanationRequest { get; set; }
    public string? ExplanationRequestedBy { get; set; }
    public DateTime? ExplanationRequestedAt { get; set; }
    public string? ExplanationText { get; set; }
    public string? ExplanationDoc { get; set; }
    public string? ExplainedBy { get; set; }
    public DateTime? ExplainedAt { get; set; }

    public virtual TbMeetingAgenda AgendaR { get; set; } = null!;

    public virtual TbMeetingSchedule MeetingR { get; set; } = null!;
}
