using CMOmeets.Application.Agendas;
using CMOmeets.Application.Common;
using CMOmeets.Domain.Data;
using CMOmeets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Meetings;

public class MeetingsService
{
    private readonly CmoMeetsDbContext _db;
    public MeetingsService(CmoMeetsDbContext db) => _db = db;


    public async Task<List<MeetingListDto>> GetMeetingsAsync(
        bool includeInactive = false,
        ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);

        var query = _db.TbMeetingSchedules.AsQueryable();

        if (!includeInactive)
            query = query.Where(m => m.Active == "Y");

        if (scopeRids is not null)
        {
            query = query.Where(m =>
                _db.TbMeetingMembers.Any(mm =>
                    mm.MeetingRid == m.Rid &&
                    scopeRids.Contains(mm.MemberRid)));
        }

        var meetings = await query
            .OrderByDescending(m => m.MeetingDate)
            .Select(m => new
            {
                m.Rid,
                m.MeetingDate,
                m.MeetingPlace,
                m.MeetingSubject,
                HasDoc = m.MeetingDocument != null && m.MeetingDocument != "",
                IsActive = m.Active == "Y"
            })
            .ToListAsync();

        var ids = meetings.Select(m => m.Rid).ToList();

        // Only ACTIVE officers are considered as meeting members.
        // Inactive officers' old TbMeetingMembers rows remain in DB,
        // but they are excluded from member count and meeting data.
        var members = await (
            from mm in _db.TbMeetingMembers
            join o in _db.TblOfficers
                on mm.MemberRid equals o.Rid
            where ids.Contains(mm.MeetingRid)
                  && o.Active == "Y"
            select new
            {
                mm.MeetingRid,
                mm.MemberRid
            }
        ).ToListAsync();

        var agendas = await _db.TbMeetingAgendas
            .Where(a =>
                a.Active == "Y" &&
                ids.Contains(a.MeetingRid))
            .Select(a => new
            {
                a.Rid,
                a.MeetingRid,
                a.MemberRids,
                a.AgendaDueDt,
                a.AgendaStatus,

                // Whether a concerned department has opened (expanded) this point.
                Opened = _db.TbActionPointViews.Any(v =>
                    v.AgendaRid == a.Rid)
            })
            .ToListAsync();

        // A scoped login (department or officer) counts only its own
        // active members and action points per meeting.
        if (scopeRids is not null)
        {
            members = members
                .Where(mm => scopeRids.Contains(mm.MemberRid))
                .ToList();

            agendas = agendas
                .Where(a =>
                    ParseRids(a.MemberRids)
                        .Any(scopeRids.Contains))
                .ToList();
        }

        var memberCounts = members
            .GroupBy(mm => mm.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        // Classify every point with the app-wide status signal (PointEvaluator).
        var todayD = DateOnly.FromDateTime(DateTime.Today);
        var todayNum = todayD.DayNumber;

        var evalMap = await PointEvaluator.EvaluateManyAsync(
            _db,
            agendas.Select(a =>
                new AgendaHeader(
                    a.Rid,
                    a.MemberRids,
                    a.AgendaDueDt,
                    a.AgendaStatus))
            .ToList(),
            todayD);

        var classified = agendas
            .Select(a =>
            {
                var (status, progress) = evalMap[a.Rid];

                return new
                {
                    a.MeetingRid,
                    Status = status,
                    EffProgress = progress,
                    a.Opened,

                    // Days past the due date for overdue points.
                    OverdueDays =
                        status == "OverDue" &&
                        a.AgendaDueDt is DateOnly due
                            ? todayNum - due.DayNumber
                            : 0
                };
            })
            .ToList();

        var pointCounts = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        var completedCounts = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a => a.Status == "Completed"));

        var overDueCounts = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a => a.Status == "OverDue"));

        var od0To7 = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a =>
                    a.Status == "OverDue" &&
                    a.OverdueDays <= 7));

        var od8To30 = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a =>
                    a.Status == "OverDue" &&
                    a.OverdueDays > 7 &&
                    a.OverdueDays <= 30));

        var od31To60 = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a =>
                    a.Status == "OverDue" &&
                    a.OverdueDays > 30 &&
                    a.OverdueDays <= 60));

        var od60Plus = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a =>
                    a.Status == "OverDue" &&
                    a.OverdueDays > 60));

        var inProgressCounts = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a => a.Status == "InProgress"));

        var progressPct = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => (int)Math.Round(
                    g.Average(a => (double)a.EffProgress)));

        var openedCounts = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => g.Count(a => a.Opened));

        var scoreByMeeting = classified
            .GroupBy(a => a.MeetingRid)
            .ToDictionary(
                g => g.Key,
                g => MeetingScoreHelper.Compute(
                    g.Count(),
                    g.Count(a => a.Status == "Completed"),
                    g.Count(a => a.Status == "InProgress"),
                    g.Count(a => a.Status == "OverDue")));

        return meetings
            .Select(m =>
            {
                var total = pointCounts.GetValueOrDefault(m.Rid);
                var completed = completedCounts.GetValueOrDefault(m.Rid);

                return new MeetingListDto(
                    m.Rid,
                    m.MeetingDate,
                    m.MeetingPlace,
                    m.MeetingSubject,
                    m.HasDoc,

                    // Count only ACTIVE officers.
                    memberCounts.GetValueOrDefault(m.Rid),

                    total,
                    completed,
                    inProgressCounts.GetValueOrDefault(m.Rid),
                    overDueCounts.GetValueOrDefault(m.Rid),

                    od0To7.GetValueOrDefault(m.Rid),
                    od8To30.GetValueOrDefault(m.Rid),
                    od31To60.GetValueOrDefault(m.Rid),
                    od60Plus.GetValueOrDefault(m.Rid),

                    progressPct.GetValueOrDefault(m.Rid),
                    openedCounts.GetValueOrDefault(m.Rid),
                    scoreByMeeting.GetValueOrDefault(m.Rid),

                    m.IsActive);
            })
            .ToList();
    }

    public async Task<MeetingDetailDto?> GetMeetingAsync(int id, ScopeRequest? scope = null)
    {
        var m = await _db.TbMeetingSchedules.FirstOrDefaultAsync(x => x.Rid == id);
        if (m is null) return null;
        // A scoped login (department or officer) may only open a meeting it is part of.
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        if (scopeRids is not null &&
            !await _db.TbMeetingMembers.AnyAsync(mm => mm.MeetingRid == id && scopeRids.Contains(mm.MemberRid)))
            return null;

        var memberRows = await (
    from mm in _db.TbMeetingMembers
    join o in _db.TblOfficers on mm.MemberRid equals o.Rid
    where mm.MeetingRid == id
       && o.Active == "Y"
       && (
            o.OfficerDesignations.Any(x =>
                x.Active == "Y" &&
                x.DesigId == mm.DesignationId &&
                x.Desig != null &&
                x.Desig.DesigName != null &&
                x.Desig.DesigName != ""
            )
            ||
            (
                o.DesigId != null &&
                o.Desig != null &&
                o.Desig.DesigName != null &&
                o.Desig.DesigName != ""
            )
       )
    orderby o.Dept.DepartmentName, o.Desig!.SeqNo
    select new
    {
        o.Rid,
        o.OfficerName,

        LegacyDeptId = o.DeptId,
        LegacyDept = o.Dept.DepartmentName,
        LegacyDesigId = o.DesigId,
        LegacyDesig = o.Desig!.DesigName,

        Posts = o.OfficerDesignations
            .Where(x =>
                x.Active == "Y" &&
                x.DesigId == mm.DesignationId &&
                x.Desig != null &&
                x.Desig.DesigName != null &&
                x.Desig.DesigName != ""
            )
            .Select(x => new MemberPostDto(
                x.Desig.DeptId,
                x.Desig.Dept.DepartmentName,
                x.DesigId,
                x.Desig.DesigName))
            .ToList()
    }
).ToListAsync();
        var members = memberRows.Select(r =>
        {
            // Officers predating the junction tables have no post rows — fall back to the legacy
            // columns so they read exactly as before.
            var posts = r.Posts.Count > 0
                ? r.Posts.OrderBy(p => p.DepartmentName).ToList()
                : new List<MemberPostDto> { new(r.LegacyDeptId, r.LegacyDept, r.LegacyDesigId ?? 0, r.LegacyDesig ?? "") };

            // No post outranks another, so with no department in hand the flat pair lists them all
            // rather than picking whichever one the legacy DeptId column happens to hold.
            var desig = string.Join(", ", posts.Select(p => p.DesigName).Where(n => !string.IsNullOrEmpty(n)).Distinct());
            var dept = string.Join(", ", posts.Select(p => p.DepartmentName).Distinct());

            // A department-scoped caller (nodal login, or an admin/cm ?deptId drill-down) sees the
            // officer wearing THAT department's hat — the post they hold there, not their primary.
            if (scope?.DeptFilter is int scopedDeptId &&
                posts.FirstOrDefault(p => p.DeptId == scopedDeptId) is MemberPostDto scopedPost)
            {
                desig = scopedPost.DesigName;
                dept = scopedPost.DepartmentName;
                posts = new List<MemberPostDto> { scopedPost };
            }

            return new MeetingMemberDto(r.Rid, r.OfficerName, desig, dept, posts);
        }).ToList();

        return new MeetingDetailDto(m.Rid, m.MeetingDate, m.MeetingPlace, m.MeetingSubject,
            m.MeetingDocument, m.Active == "Y", members);
    }

    // Loads a meeting only if the caller is entitled to it: admin/cm (unscoped) always, nodal/officer
    // only when they are a member — the same rule GetMeetingAsync enforces. Returns null otherwise.
    private async Task<TbMeetingSchedule?> FindInScopeAsync(int id, ScopeRequest? scope)
    {
        var m = await _db.TbMeetingSchedules.FirstOrDefaultAsync(x => x.Rid == id);
        if (m is null) return null;
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        if (scopeRids is not null &&
            !await _db.TbMeetingMembers.AnyAsync(mm => mm.MeetingRid == id && scopeRids.Contains(mm.MemberRid)))
            return null;
        return m;
    }

    // True when the caller may act on this meeting (used to authorise a document upload before any
    // file is written to disk).
    public async Task<bool> MeetingInScopeAsync(int id, ScopeRequest? scope = null)
        => await FindInScopeAsync(id, scope) is not null;

    // The meeting's Minutes-of-Meeting document filename, or null when there is none or the caller is
    // out of scope. Drives the scoped, view-only download endpoint.
    public async Task<string?> GetMeetingDocumentAsync(int id, ScopeRequest? scope = null)
        => (await FindInScopeAsync(id, scope))?.MeetingDocument;

    // Attach (or replace) the meeting's Minutes-of-Meeting document. `storedName` is the opaque filename
    // already written to disk by the file service. Returns the PREVIOUS document filename (or null) so
    // the caller can delete the now-orphaned file; returns null too when out of scope / not found.
    public async Task<string?> SetMeetingDocumentAsync(int id, string storedName, ScopeRequest? scope = null)
    {
        var m = await FindInScopeAsync(id, scope);
        if (m is null) return null;
        var previous = m.MeetingDocument;
        m.MeetingDocument = storedName;
        await _db.SaveChangesAsync();
        return previous;
    }
    public async Task<MeetingListDto> CreateMeetingAsync(
    MeetingSaveDto dto,
    string actor)
    {
        var entity = new TbMeetingSchedule
        {
            MeetingDate = dto.MeetingDate,
            MeetingPlace = dto.MeetingPlace.Trim(),
            MeetingSubject = dto.MeetingSubject,
            MeetingDocument = dto.MeetingDocument,
            Active = "Y",
            AddedAt = DateTime.Now,
            AddedBy = actor
        };

        _db.TbMeetingSchedules.Add(entity);
        await _db.SaveChangesAsync();

        //object value = await SyncMembersAsync(entity.Rid, dto.SelectedOfficers);
        await SyncMembersAsync(entity.Rid, dto.SelectedOfficers);

        await _db.SaveChangesAsync();

        return (await GetMeetingsAsync(true))
            .First(x => x.Rid == entity.Rid);
    }

    //public async Task<MeetingListDto> CreateMeetingAsync(MeetingSaveDto dto, string actor)
    //{
    //    var entity = new TbMeetingSchedule
    //    {
    //        MeetingDate = dto.MeetingDate,
    //        MeetingPlace = dto.MeetingPlace.Trim(),
    //        MeetingSubject = dto.MeetingSubject,
    //        MeetingDocument = dto.MeetingDocument,
    //        Active = "Y",
    //        AddedAt = DateTime.Now,
    //        AddedBy = actor
    //    };
    //    _db.TbMeetingSchedules.Add(entity);
    //    await _db.SaveChangesAsync();

    //    await SyncMembersAsync(entity.Rid, dto.SelectedOfficers);
    //    await _db.SaveChangesAsync();

    //    return (await GetMeetingsAsync(true)).First(x => x.Rid == entity.Rid);
    //}

    //public async Task<bool> UpdateMeetingAsync(int id, MeetingSaveDto dto)
    //{
    //    var entity = await _db.TbMeetingSchedules.FindAsync(id);
    //    if (entity is null) return false;

    //    entity.MeetingDate = dto.MeetingDate;
    //    entity.MeetingPlace = dto.MeetingPlace.Trim();
    //    entity.MeetingSubject = dto.MeetingSubject;
    //    if (dto.MeetingDocument is not null) entity.MeetingDocument = dto.MeetingDocument;

    //    await SyncMembersAsync(entity.Rid, dto.SelectedOfficers);
    //    await _db.SaveChangesAsync();
    //    return true;
    //}
    public async Task<bool> UpdateMeetingAsync(
    int id,
    MeetingSaveDto dto)
    {
        var entity = await _db.TbMeetingSchedules.FindAsync(id);

        if (entity is null)
            return false;

        // Update meeting details
        entity.MeetingDate = dto.MeetingDate;

        entity.MeetingPlace = dto.MeetingPlace.Trim();

        entity.MeetingSubject = dto.MeetingSubject;

        if (dto.MeetingDocument is not null)
        {
            entity.MeetingDocument = dto.MeetingDocument;
        }

        await SyncMembersAsync(
            entity.Rid,
            dto.SelectedOfficers);

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMeetingAsync(int id)
    {
        var entity = await _db.TbMeetingSchedules.FindAsync(id);
        if (entity is null) return false;
        entity.Active = "N";
        await _db.SaveChangesAsync();
        return true;
    }

    // Pending-minutes for a nodal login: the meetings of its department that still have no action point
    // for any of the department's officers. Resolves the department's officer rids, then delegates.
    public async Task<List<PendingMinuteDto>> GetPendingMinutesForScopeAsync(ScopeRequest scope)
    {
        var rids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        return await GetPendingMinutesMeetingsAsync(rids ?? new List<int>());
    }

    // Meetings involving the given member officers — a single officer (officer login) or a whole
    // department's officers (nodal login) — that are not yet complete: still missing an action point for
    // any of them, OR the minutes document. Drives the "Pending Upload Minutes" screen + nav blink.
    public async Task<List<PendingMinuteDto>> GetPendingMinutesMeetingsAsync(IReadOnlyCollection<int> memberRids)
    {
        if (memberRids.Count == 0) return new List<PendingMinuteDto>();
        var idList = memberRids as List<int> ?? memberRids.ToList();

        var meetingIds = await _db.TbMeetingMembers
            .Where(mm => idList.Contains(mm.MemberRid))
            .Select(mm => mm.MeetingRid).Distinct().ToListAsync();
        if (meetingIds.Count == 0) return new List<PendingMinuteDto>();

        // Meetings that already have an active action point naming one of these officers (their rid in
        // MemberRids). MemberRids is a CSV, so the rid-in-list check is done in memory via ParseRids.
        var agendaRids = await _db.TbMeetingAgendas
            .Where(a => a.Active == "Y" && meetingIds.Contains(a.MeetingRid))
            .Select(a => new { a.MeetingRid, a.MemberRids })
            .ToListAsync();
        var coveredMeetingIds = agendaRids
            .Where(a => ParseRids(a.MemberRids).Any(memberRids.Contains))
            .Select(a => a.MeetingRid)
            .ToHashSet();

        // A meeting is only "done" when it has BOTH an action point (for these officers) AND a minutes
        // document. It keeps appearing (and the nav item keeps blinking) until both are present.
        var meetings = await _db.TbMeetingSchedules
            .Where(m => m.Active == "Y" && meetingIds.Contains(m.Rid))
            .OrderByDescending(m => m.MeetingDate)
            .Select(m => new { m.Rid, m.MeetingDate, m.MeetingPlace, m.MeetingSubject, m.MeetingDocument })
            .ToListAsync();

        var pending = meetings
            .Select(m => new
            {
                m.Rid,
                m.MeetingDate,
                m.MeetingPlace,
                m.MeetingSubject,
                HasActionPoint = coveredMeetingIds.Contains(m.Rid),
                HasDocument = !string.IsNullOrEmpty(m.MeetingDocument)
            })
            .Where(m => !m.HasActionPoint || !m.HasDocument)
            .ToList();
        if (pending.Count == 0) return new List<PendingMinuteDto>();

        var pendingRids = pending.Select(p => p.Rid).ToList();
        var memberCounts = (await _db.TbMeetingMembers
            .Where(mm => pendingRids.Contains(mm.MeetingRid))
            .GroupBy(mm => mm.MeetingRid)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync())
            .ToDictionary(x => x.Key, x => x.Count);

        return pending.Select(p => new PendingMinuteDto(
            p.Rid, p.MeetingDate, p.MeetingPlace, p.MeetingSubject,
            memberCounts.GetValueOrDefault(p.Rid), p.HasActionPoint, p.HasDocument)).ToList();
    }

    // True when the meeting has at least one member officer serving the given department. An officer's
    // departments are a flat set (no primary), so this asks the officer-department junction and only
    // falls back to the legacy DeptId column for officers with no junction rows.
    public Task<bool> DeptOwnsMeetingAsync(int meetingId, int deptId) =>
        _db.TbMeetingMembers
            .Where(mm => mm.MeetingRid == meetingId)
            .Join(_db.TblOfficers, mm => mm.MemberRid, o => o.Rid, (mm, o) => o)
            .AnyAsync(o => o.OfficerDepartments.Any(x => x.Active == "Y")
                ? o.OfficerDepartments.Any(x => x.Active == "Y" && x.DeptId == deptId)
                : o.DeptId == deptId);
    private async Task SyncMembersAsync(
    int meetingId,
    List<SelectedOfficerDto> selectedOfficers)
    {
        // Get existing members for this meeting
        var existing = await _db.TbMeetingMembers
            .Where(m => m.MeetingRid == meetingId)
            .ToListAsync();

        // Remove existing members
        _db.TbMeetingMembers.RemoveRange(existing);

        // Add selected officers
        foreach (var officer in selectedOfficers
            .Where(x => x.DesignationId != 0)
            .DistinctBy(x => new
            {
                x.OfficerId,
                x.DesignationId,
                x.DepartmentId
            }))
        {
            _db.TbMeetingMembers.Add(new TbMeetingMember
            {
                MeetingRid = meetingId,
                MemberRid = officer.OfficerId,
                DesignationId = officer.DesignationId,

                // NEW: Department ID save
                DepartmentId = officer.DepartmentId,

                AddedAt = DateTime.Now
            });
        }
    }

    //private async Task SyncMembersAsync(int meetingId, List<SelectedOfficerDto> selectedOfficers)
    //{
    //    var existing = await _db.TbMeetingMembers.Where(m => m.MeetingRid == meetingId).ToListAsync();
    //    _db.TbMeetingMembers.RemoveRange(existing);
    //    foreach (var oid in officerIds.Distinct())
    //        _db.TbMeetingMembers.Add(new TbMeetingMember { MeetingRid = meetingId, MemberRid = oid, AddedAt = DateTime.Now });
    //}
    //private async Task SyncMembersAsync(
    //int meetingId,
    //List<SelectedOfficerDto> selectedOfficers)
    //{
    //    var existing = await _db.TbMeetingMembers
    //        .Where(m => m.MeetingRid == meetingId)
    //        .ToListAsync();

    //    _db.TbMeetingMembers.RemoveRange(existing);

    //foreach (var officer in selectedOfficers.DistinctBy(x => x.OfficerId))
    //{
    //    _db.TbMeetingMembers.Add(new TbMeetingMember
    //    {
    //        MeetingRid = meetingId,
    //        MemberRid = officer.OfficerId,
    //        DesignationId = officer.DesignationId,
    //        AddedAt = DateTime.Now
    //    });
    //}
    //    foreach (var officer in selectedOfficers.Where(x => x.DesignationId != 0).DistinctBy(x => new { x.OfficerId, x.DesignationId }))
    //    {
    //        _db.TbMeetingMembers.Add(new TbMeetingMember
    //        {
    //            MeetingRid = meetingId,
    //            MemberRid = officer.OfficerId,
    //            DesignationId = officer.DesignationId,
    //            AddedAt = DateTime.Now
    //        });
    //    }
    //}

    private static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }
}
