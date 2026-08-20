using CMOmeets.Application.Common;

namespace CMOmeets.Application.Agendas;

// Members carries the same concerned officers as the AgendaMembers name snapshot, but resolved
// against the officer master so each name comes with its current mobile number.
public record AgendaDto(
    long Rid, int MeetingRid, string MeetingAgenda, string? AgendaMembers, string? MemberRids,
    DateOnly? AgendaDueDt, string DistrictName, string Status, int RemarksCount, int? Progress, DateTime? AddedAt,
    bool Opened, DateTime? FirstViewedAt, DateTime? LastViewedAt, string? ViewedBy,
    List<OfficerContactDto> Members,
    bool IsOfficerCalled, string? OfficerRemark);

// Admin-only follow-up on a point's concerned officer. OfficerRemark is only kept while
// IsOfficerCalled is true.
public record OfficerCallSaveDto(bool IsOfficerCalled, string? OfficerRemark);

public record AgendaSaveDto(
    int MeetingRid, string MeetingAgenda, List<int> MemberOfficerIds,
    DateOnly? AgendaDueDt, string DistrictName);

public record RemarkDto(
    long Rid, long AgendaRid, int MemberRid, string MemberName, string AgendaRemarks,
    DateOnly? RemarksDate, string? RemarkStatus, int? ProgressPercentage, string? AtrDoc, DateTime? AddedAt,
    string? ExplanationRequest, DateTime? ExplanationRequestedAt,
    string? ExplanationText, string? ExplanationDoc, DateTime? ExplainedAt);

public record ExplanationRequestDto(string Request);
public record ExplanationReplyDto(string Explanation, string? Doc);

public record RemarkSaveDto(
    long AgendaRid, int MemberRid, string MemberName, string AgendaRemarks,
    DateOnly? RemarksDate, string RemarkStatus, int? ProgressPercentage, string? AtrDoc, DateOnly? AgendaDueDt);

public record ActionablePointDto(
    long AgendaId, int MeetingRid, string MeetingSubject, DateTime MeetingDate, string MeetingPlace,
    string MeetingAgenda, string? AgendaMembers, string DistrictName, DateOnly? AgendaDueDt,
    int DueDays, string Status, int RemarksCount, int? Progress, List<OfficerContactDto> Members,
    bool IsOfficerCalled, string? OfficerRemark);

// One row of an action point's activity timeline (creation → opens → ATRs → explanations).
public record ActivityLogEntryDto(string Subject, DateTime When);
