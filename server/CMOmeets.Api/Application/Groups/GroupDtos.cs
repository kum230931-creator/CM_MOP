namespace CMOmeets.Application.Groups;

public record GroupDto(long Rid, string GroupName, bool Active, int MeetingCount);
public record GroupSaveDto(string GroupName, bool Active);
public record GroupMeetingDto(int MeetingRid, DateTime MeetingDate, string MeetingPlace, string? MeetingSubject);
public record MapMeetingsDto(List<int> MeetingIds);
