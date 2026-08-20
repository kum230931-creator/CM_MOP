using CMOmeets.Application.Common;

namespace CMOmeets.Application.Dashboard;

public record DashboardCountersDto(
    int TotalMeetings, int TotalPoints, int Completed, int InProgress, int OverDue);

// Members = the AgendaMembers names resolved against the officer master, so each carries the
// mobile number shown (and dialled) next to it.
public record DashboardPointDto(
    long AgendaId, int MeetingRid, string MeetingSubject, DateTime MeetingDate,
    string MeetingAgenda, string? AgendaMembers, string DistrictName,
    DateOnly? AgendaDueDt, int DueDays, string Status, List<OfficerContactDto> Members);

public record MeetingAbstractDto(
    int MeetingRid, string? MeetingSubject, DateTime MeetingDate,
    int TotalPoints, int Completed, int InProgress, int OverDue,
    // Overdue points split by how many days past the due date they are.
    int OverDue0To7, int OverDue8To30, int OverDue31To60, int OverDue60Plus, double Score);

public record DepartmentStatDto(
    int DeptId, string DeptName, int TotalPoints,
    int Completed, int InProgress, int OverDue, int CompletionRate, double AvgMeetingScore);

public record OfficerStatDto(
    int OfficerId, string OfficerName, string? Designation, string? DepartmentName, int TotalPoints,
    int Completed, int InProgress, int OverDue, int CompletionRate, double AvgMeetingScore);
