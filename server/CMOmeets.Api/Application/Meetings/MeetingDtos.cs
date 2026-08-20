namespace CMOmeets.Application.Meetings;

public record MeetingListDto(
    int Rid, DateTime MeetingDate, string MeetingPlace, string? MeetingSubject,
    bool HasDocument, int MemberCount, int AgendaCount,
    int CompletedCount, int InProgressCount, int OverDueCount,
    // Overdue points split by how many days past the due date they are (grouped "Overdue" columns).
    int OverDue0To7, int OverDue8To30, int OverDue31To60, int OverDue60Plus,
    int ProgressPercent, int OpenedCount, double Score, bool Active);

public record MeetingDetailDto(
    int Rid, DateTime MeetingDate, string MeetingPlace, string? MeetingSubject,
    string? MeetingDocument, bool Active, List<MeetingMemberDto> Members);

// One department "hat" a meeting member wears: a department they serve and the designation (post)
// they hold there. Departments are a flat set with no primary, so a multi-department officer has one
// of these per department and none of them outranks another.
public record MemberPostDto(int DeptId, string DepartmentName, int DesigId, string DesigName);

// DesigName/DepartmentName echo the post relevant to the caller: the department-scoped one for a
// nodal login or a ?deptId drill-down, otherwise every post the officer holds. Posts carries the set.
public record MeetingMemberDto(int OfficerId, string OfficerName, string DesigName, string DepartmentName,
    List<MemberPostDto> Posts);

// A meeting that still needs its minutes completed: it keeps appearing (and the nav item blinks) until
// it has BOTH an action point recorded AND a minutes document uploaded. The two flags say what's missing.
public record PendingMinuteDto(int Rid, DateTime MeetingDate, string MeetingPlace, string? MeetingSubject,
    int MemberCount, bool HasActionPoint, bool HasDocument);

public record SelectedOfficerDto(
    int OfficerId,
    int DepartmentId,
    int DesignationId
);
public record MeetingSaveDto(
    DateTime MeetingDate, string MeetingPlace, string? MeetingSubject,
    string? MeetingDocument, List<SelectedOfficerDto> SelectedOfficers);
