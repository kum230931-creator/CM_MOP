using CMOmeets.Domain.Data;
using CMOmeets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Groups;

public class GroupsService
{
    private readonly CmoMeetsDbContext _db;
    public GroupsService(CmoMeetsDbContext db) => _db = db;

    private static bool ToBool(string? f) => string.Equals(f?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
    private static string ToFlag(bool v) => v ? "Y" : "N";

    public async Task<List<GroupDto>> GetGroupsAsync() =>
        await _db.TbMeetingGroups
            .OrderBy(g => g.GroupName)
            .Select(g => new GroupDto(g.Rid, g.GroupName, g.Active == "Y",
                _db.TbMeetingMappedGroups.Count(m => m.GroupRid == g.Rid && m.Active == "Y")))
            .ToListAsync();

    public async Task<GroupDto> CreateGroupAsync(GroupSaveDto dto, string actor)
    {
        var entity = new TbMeetingGroup
        {
            GroupName = dto.GroupName.Trim(),
            Active = ToFlag(dto.Active),
            AddedAt = DateTime.Now,
            AddedBy = actor
        };
        _db.TbMeetingGroups.Add(entity);
        await _db.SaveChangesAsync();
        return new GroupDto(entity.Rid, entity.GroupName, entity.Active == "Y", 0);
    }

    public async Task<bool> UpdateGroupAsync(long id, GroupSaveDto dto)
    {
        var entity = await _db.TbMeetingGroups.FindAsync(id);
        if (entity is null) return false;
        entity.GroupName = dto.GroupName.Trim();
        entity.Active = ToFlag(dto.Active);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGroupAsync(long id)
    {
        var entity = await _db.TbMeetingGroups.FindAsync(id);
        if (entity is null) return false;
        entity.Active = "N";
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<GroupMeetingDto>> GetGroupMeetingsAsync(long groupId) =>
        await (
            from map in _db.TbMeetingMappedGroups
            join m in _db.TbMeetingSchedules on map.MeetingRid equals m.Rid
            where map.GroupRid == groupId && map.Active == "Y"
            orderby m.MeetingDate descending
            select new GroupMeetingDto(m.Rid, m.MeetingDate, m.MeetingPlace, m.MeetingSubject)
        ).ToListAsync();

    public async Task SetGroupMeetingsAsync(long groupId, List<int> meetingIds, string actor)
    {
        var existing = await _db.TbMeetingMappedGroups.Where(m => m.GroupRid == groupId).ToListAsync();
        _db.TbMeetingMappedGroups.RemoveRange(existing);
        foreach (var mid in meetingIds.Distinct())
            _db.TbMeetingMappedGroups.Add(new TbMeetingMappedGroup
            {
                GroupRid = groupId, MeetingRid = mid, Active = "Y", AddedAt = DateTime.Now, AddedBy = actor
            });
        await _db.SaveChangesAsync();
    }
}
