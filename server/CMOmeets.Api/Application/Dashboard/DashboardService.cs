using CMOmeets.Application.Agendas;
using CMOmeets.Application.Common;
using CMOmeets.Application.Meetings;
using CMOmeets.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Dashboard;

public class DashboardService
{
    private readonly CmoMeetsDbContext _db;
    public DashboardService(CmoMeetsDbContext db) => _db = db;

    private record AgendaRow(
    long Rid,
    int MeetingRid,
    string? MeetingSubject,
    DateTime MeetingDate,
    string MeetingAgenda,
    string? AgendaMembers,
    string? MemberRids,
    string DistrictName,
    DateOnly? AgendaDueDt,
    string AgendaStatus,
    string? DepartmentIDs);

    //private async Task<List<AgendaRow>> LoadAsync(long? groupId, ScopeRequest? scope)
    //{
    //    var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
    //    var query =
    //        from a in _db.TbMeetingAgendas
    //        join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
    //        where a.Active == "Y" && m.Active == "Y"
    //        select new AgendaRow(a.Rid, a.MeetingRid, m.MeetingSubject, m.MeetingDate,
    //            a.MeetingAgenda, a.AgendaMembers, a.MemberRids, a.DistrictName, a.AgendaDueDt, a.AgendaStatus);

    //    if (groupId is not null)
    //    {
    //        var meetingIds = await _db.TbMeetingMappedGroups
    //            .Where(g => g.GroupRid == groupId && g.Active == "Y")
    //            .Select(g => g.MeetingRid).ToListAsync();
    //        query = query.Where(r => meetingIds.Contains(r.MeetingRid));
    //    }

    //    var rows = await query.ToListAsync();

    //    // A scoped login (department or officer) only counts action points involving its own officer(s).
    //    if (scopeRids is not null)
    //        rows = rows.Where(r => ParseRids(r.MemberRids).Any(scopeRids.Contains)).ToList();
    //    return rows;
    //}
    private async Task<List<AgendaRow>> LoadAsync(
    long? groupId,
    ScopeRequest? scope)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);

        // NORMAL OFFICER:
        // Only the logged-in officer's own RID should be considered.
        // Nodal / CMO behavior remains unchanged.
        if (scope?.IsOfficer == true &&
            scope.OfficerLoginId is int officerId)
        {
            scopeRids = new List<int> { officerId };
        }

        var query =
            from a in _db.TbMeetingAgendas
            join m in _db.TbMeetingSchedules
                on a.MeetingRid equals m.Rid
            where a.Active == "Y" && m.Active == "Y"
            select new AgendaRow(
                a.Rid,
                a.MeetingRid,
                m.MeetingSubject,
                m.MeetingDate,
                a.MeetingAgenda,
                a.AgendaMembers,
                a.MemberRids,
                a.DistrictName,
                a.AgendaDueDt,
                a.AgendaStatus,
                a.DepartmentIDs);

        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g =>
                    g.GroupRid == groupId &&
                    g.Active == "Y")
                .Select(g => g.MeetingRid)
                .ToListAsync();

            query = query.Where(
                r => meetingIds.Contains(r.MeetingRid));
        }

        var rows = await query.ToListAsync();

        // A scoped login counts action points involving its scoped officer(s).
        if (scopeRids is not null)
        {
            rows = rows
                .Where(r =>
                    ParseRids(r.MemberRids)
                        .Any(scopeRids.Contains))
                .ToList();
        }

        // NEW: officer login + a specific department selected from the dropdown
        // -> keep only points where THIS officer's own involvement is in that department.
        if (scope?.IsOfficer == true &&
            scope.OfficerLoginId is int off &&
            scope.DeptFilter is int deptFilter)
        {
            rows = rows
                .Where(r => DeptForOfficer(r.DepartmentIDs, off) == deptFilter)
                .ToList();
        }

        return rows;
    }
    // Per-officer status/progress for a set of loaded points (shared by the counter/list endpoints):
    // a point is Completed only when every responsible officer has completed. See PointEvaluator.
    private Task<Dictionary<long, (string Status, int Progress)>> EvaluateAsync(List<AgendaRow> rows) =>
        PointEvaluator.EvaluateManyAsync(_db,
            rows.Select(r => new AgendaHeader(r.Rid, r.MemberRids, r.AgendaDueDt, r.AgendaStatus)).ToList(),
            DateOnly.FromDateTime(DateTime.Today));

    public async Task<DashboardCountersDto> GetCountersAsync(long? groupId = null, ScopeRequest? scope = null)
    {
        var rows = await LoadAsync(groupId, scope);
        var evalMap = await EvaluateAsync(rows);
        var withStatus = rows.Select(r => evalMap[r.Rid].Status).ToList();
        return new DashboardCountersDto(
            await CountMeetingsAsync(groupId, scope),
            rows.Count,
            withStatus.Count(s => s == "Completed"),
            withStatus.Count(s => s == "InProgress"),
            withStatus.Count(s => s == "OverDue"));
    }

    // Count active meetings. A meeting is counted even before it has any action points;
    // for a department login only meetings that include one of its officers are counted.
    //private async Task<int> CountMeetingsAsync(long? groupId, ScopeRequest? scope)
    //{
    //    var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
    //    var query = _db.TbMeetingSchedules.Where(m => m.Active == "Y");
    //    if (groupId is not null)
    //    {
    //        var meetingIds = await _db.TbMeetingMappedGroups
    //            .Where(g => g.GroupRid == groupId && g.Active == "Y")
    //            .Select(g => g.MeetingRid).ToListAsync();
    //        query = query.Where(m => meetingIds.Contains(m.Rid));
    //    }
    //    if (scopeRids is not null)
    //        query = query.Where(m => _db.TbMeetingMembers.Any(mm => mm.MeetingRid == m.Rid && scopeRids.Contains(mm.MemberRid)));
    //    return await query.CountAsync();
    //}
    private async Task<int> CountMeetingsAsync(
    long? groupId,
    ScopeRequest? scope)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);

        // NORMAL OFFICER:
        // Only count meetings belonging to the logged-in officer.
        // Nodal / CMO behavior remains unchanged.
        if (scope?.IsOfficer == true &&
            scope.OfficerLoginId is int officerId)
        {
            scopeRids = new List<int> { officerId };
        }

        var query = _db.TbMeetingSchedules
            .Where(m => m.Active == "Y");

        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g =>
                    g.GroupRid == groupId &&
                    g.Active == "Y")
                .Select(g => g.MeetingRid)
                .ToListAsync();

            query = query.Where(
                m => meetingIds.Contains(m.Rid));
        }

        if (scopeRids is not null)
        {
            query = query.Where(m =>
                _db.TbMeetingMembers.Any(mm =>
                    mm.MeetingRid == m.Rid &&
                    scopeRids.Contains(mm.MemberRid) &&
                    (scope!.DeptFilter == null || mm.DepartmentId == scope.DeptFilter)));
        }

        return await query.CountAsync();
    }

    public async Task<List<DashboardPointDto>> GetOverduePointsAsync(long? groupId = null, ScopeRequest? scope = null)
        => await GetPointsByStatusAsync("OverDue", groupId, scope);

    public async Task<List<DashboardPointDto>> GetTodayDuePointsAsync(long? groupId = null, ScopeRequest? scope = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = await LoadAsync(groupId, scope);
        var evalMap = await EvaluateAsync(rows);
        var due = rows
            .Where(r => evalMap[r.Rid].Status != "Completed" && r.AgendaDueDt == today)
            .ToList();
        var contacts = await OfficerContacts.LoadAsync(_db, due.Select(r => r.MemberRids));
        return due.Select(r => ToPoint(r, today, evalMap[r.Rid].Status, contacts)).ToList();
    }

    // Points falling due over the coming week. Today's points have their own list, so the window
    // starts tomorrow (tomorrow .. today+7) and the two never show the same point twice.
    public async Task<List<DashboardPointDto>> GetUpcomingDuePointsAsync(long? groupId = null, ScopeRequest? scope = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var until = today.AddDays(7);
        var rows = await LoadAsync(groupId, scope);
        var evalMap = await EvaluateAsync(rows);
        var due = rows
            .Where(r => evalMap[r.Rid].Status != "Completed"
                        && r.AgendaDueDt is DateOnly d && d > today && d <= until)
            .OrderBy(r => r.AgendaDueDt)
            .ToList();
        var contacts = await OfficerContacts.LoadAsync(_db, due.Select(r => r.MemberRids));
        return due.Select(r => ToPoint(r, today, evalMap[r.Rid].Status, contacts)).ToList();
    }

    private async Task<List<DashboardPointDto>> GetPointsByStatusAsync(string status, long? groupId, ScopeRequest? scope)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = await LoadAsync(groupId, scope);
        var evalMap = await EvaluateAsync(rows);
        var matching = rows
            .Where(r => evalMap[r.Rid].Status == status)
            .OrderBy(r => r.AgendaDueDt)
            .ToList();
        var contacts = await OfficerContacts.LoadAsync(_db, matching.Select(r => r.MemberRids));
        return matching.Select(r => ToPoint(r, today, evalMap[r.Rid].Status, contacts)).ToList();
    }

    public async Task<List<MeetingAbstractDto>> GetMeetingAbstractAsync(
    long? groupId = null,
    ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var officerId = scope?.RequestedOfficerId
            ?? (scope?.IsOfficer == true ? scope.OfficerLoginId : null);

        if (officerId is int off)
            scopeRids = new List<int> { off };

        // ============================================================
        // STEP 1: MEETINGS IN SCOPE
        // Start from meetings themselves (like GetMeetingsAsync), NOT
        // from agendas -- so a meeting the officer belongs to still
        // shows up even with zero/no matching agenda points.
        // ============================================================
        var meetingQuery = _db.TbMeetingSchedules.Where(m => m.Active == "Y");

        if (groupId is not null)
        {
            var groupMeetingIds = await _db.TbMeetingMappedGroups
                .Where(g => g.GroupRid == groupId && g.Active == "Y")
                .Select(g => g.MeetingRid)
                .ToListAsync();
            meetingQuery = meetingQuery.Where(m => groupMeetingIds.Contains(m.Rid));
        }

        if (scopeRids is not null)
        {
            meetingQuery = meetingQuery.Where(m =>
                _db.TbMeetingMembers.Any(mm =>
                    mm.MeetingRid == m.Rid &&
                    scopeRids.Contains(mm.MemberRid) &&
                    (scope!.DeptFilter == null || mm.DepartmentId == scope.DeptFilter)));
        }

        var meetings = await meetingQuery
            .Select(m => new { m.Rid, m.MeetingSubject, m.MeetingDate })
            .ToListAsync();

        if (meetings.Count == 0)
            return [];

        var meetingIds = meetings.Select(m => m.Rid).ToList();

        // ============================================================
        // STEP 2: ACTIVE AGENDA POINTS FOR THOSE MEETINGS
        // ============================================================
        var agendaRows = await _db.TbMeetingAgendas
            .Where(a => a.Active == "Y" && meetingIds.Contains(a.MeetingRid))
            .Select(a => new { a.Rid, a.MeetingRid, a.MemberRids, a.AgendaDueDt, a.AgendaStatus })
            .ToListAsync();

        if (scopeRids is not null)
        {
            agendaRows = agendaRows
                .Where(r => ParseRids(r.MemberRids).Any(scopeRids.Contains))
                .ToList();
        }

        var evalMap = agendaRows.Count == 0
            ? new Dictionary<long, (string Status, int Progress)>()
            : await PointEvaluator.EvaluateManyAsync(
                _db,
                agendaRows.Select(r =>
                {
                    var rids = ParseRids(r.MemberRids);
                    var scoped = scopeRids is not null ? rids.Where(scopeRids.Contains) : rids;
                    return new AgendaHeader(r.Rid, string.Join(",", scoped), r.AgendaDueDt, r.AgendaStatus);
                }).ToList(),
                today);

        var pointsByMeeting = agendaRows.ToLookup(r => r.MeetingRid);
        int todayNum = today.DayNumber;

        // ============================================================
        // STEP 3: BUILD ABSTRACT -- ONE ROW PER MEETING, EVEN 0-POINT
        // ============================================================
        return meetings
            .Select(m =>
            {
                int comp = 0, od = 0, prog = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, total = 0;

                foreach (var r in pointsByMeeting[m.Rid])
                {
                    total++;
                    switch (evalMap[r.Rid].Status)
                    {
                        case "Completed": comp++; break;
                        case "OverDue":
                            od++;
                            int days = r.AgendaDueDt is DateOnly d ? todayNum - d.DayNumber : 0;
                            if (days <= 7) b1++;
                            else if (days <= 30) b2++;
                            else if (days <= 60) b3++;
                            else b4++;
                            break;
                        default: prog++; break;
                    }
                }

                return new MeetingAbstractDto(
                    m.Rid, m.MeetingSubject, m.MeetingDate,
                    total, comp, prog, od, b1, b2, b3, b4,
                    MeetingScoreHelper.Compute(total, comp, prog, od));
            })
            .OrderByDescending(a => a.MeetingDate)
            .ToList();
    }
    public async Task<List<DepartmentStatDto>> GetDepartmentStatsAsync(long? groupId = null, ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);

        // One pass over every active point (with its status flag) feeds both the per-department
        // counts and the per-(department, meeting) tallies used for the score.
        var pts = await (
            from a in _db.TbMeetingAgendas
            join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
            where a.Active == "Y" && m.Active == "Y"
            select new
            {
                a.Rid, a.MeetingRid, a.MemberRids, a.AgendaDueDt, a.AgendaStatus
            }).ToListAsync();

        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g => g.GroupRid == groupId && g.Active == "Y")
                .Select(g => g.MeetingRid).ToListAsync();
            pts = pts.Where(p => meetingIds.Contains(p.MeetingRid)).ToList();
        }
        // A scoped login (department or officer) only counts its own points.
        if (scopeRids is not null)
            pts = pts.Where(p => ParseRids(p.MemberRids).Any(scopeRids.Contains)).ToList();

        var evalMap = await PointEvaluator.EvaluateManyAsync(_db,
            pts.Select(p => new AgendaHeader(p.Rid, p.MemberRids, p.AgendaDueDt, p.AgendaStatus)).ToList(),
            DateOnly.FromDateTime(DateTime.Today));

        // officer rid -> (department id, department name)
        var officerDept = await _db.TblOfficers
            .Join(_db.DepartmentMas, o => o.DeptId, d => d.Rid,
                (o, d) => new { o.Rid, DeptId = d.Rid, d.DepartmentName })
            .ToDictionaryAsync(x => x.Rid, x => (x.DeptId, x.DepartmentName));

        var acc = new Dictionary<int, (string Name, int Total, int Completed, int InProgress, int OverDue)>();
        var perDeptMeeting = new Dictionary<(int Dept, int Meeting), (int Total, int Completed, int InProgress, int OverDue)>();
        foreach (var p in pts)
        {
            var status = evalMap[p.Rid].Status;
            bool completed = status == "Completed";
            bool overdue = status == "OverDue";
            var depts = ParseRids(p.MemberRids)
                .Where(officerDept.ContainsKey)
                .Select(rid => officerDept[rid])
                .Distinct();
            foreach (var (deptId, deptName) in depts)
            {
                acc.TryGetValue(deptId, out var cur);
                cur.Name = deptName;
                cur.Total++;
                if (completed) cur.Completed++;
                else if (overdue) cur.OverDue++;
                else cur.InProgress++;
                acc[deptId] = cur;

                var key = (deptId, p.MeetingRid);
                perDeptMeeting.TryGetValue(key, out var mc);
                mc.Total++;
                if (completed) mc.Completed++;
                else if (overdue) mc.OverDue++;
                else mc.InProgress++;
                perDeptMeeting[key] = mc;
            }
        }

        // Accumulated score per department = the average of its meetings' scores (the same per-meeting
        // Score shown on the Meetings / dashboard screens).
        var avgScore = perDeptMeeting
            .GroupBy(kv => kv.Key.Dept)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(kv =>
                    MeetingScoreHelper.Compute(kv.Value.Total, kv.Value.Completed, kv.Value.InProgress, kv.Value.OverDue)), 1));

        return acc
            // When scoped to one department, points it shares with other departments would
            // otherwise pull those departments in — keep only the scoped department's own stats.
            .Where(kv => scope?.DeptFilter is null || kv.Key == scope.DeptFilter)
            .Select(kv => new DepartmentStatDto(
                kv.Key, kv.Value.Name, kv.Value.Total,
                kv.Value.Completed, kv.Value.InProgress, kv.Value.OverDue,
                kv.Value.Total == 0 ? 0 : (int)Math.Round(100.0 * kv.Value.Completed / kv.Value.Total),
                avgScore.GetValueOrDefault(kv.Key)))
            .OrderByDescending(d => d.CompletionRate)
            .ThenByDescending(d => d.TotalPoints)
            .ToList();
    }

    // Per-officer performance for the admin Officer Dashboard — the officer analogue of
    // GetDepartmentStatsAsync. Every active officer in scope is listed (zeros when they have no points
    // yet); each number uses the same app-wide status signal (AgendaStatusHelper), and the accumulated
    // Score is the average of the officer's per-meeting scores (scored over that officer's own points).
    public async Task<List<OfficerStatDto>> GetOfficerStatsAsync(long? groupId = null, ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);

        // One pass over every active point (with its status flag).
        var pts = await (
            from a in _db.TbMeetingAgendas
            join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
            where a.Active == "Y" && m.Active == "Y"
            select new
            {
                a.Rid, a.MeetingRid, a.MemberRids, a.AgendaDueDt, a.AgendaStatus
            }).ToListAsync();

        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g => g.GroupRid == groupId && g.Active == "Y")
                .Select(g => g.MeetingRid).ToListAsync();
            pts = pts.Where(p => meetingIds.Contains(p.MeetingRid)).ToList();
        }
        if (scopeRids is not null)
            pts = pts.Where(p => ParseRids(p.MemberRids).Any(scopeRids.Contains)).ToList();

        var evalMap = await PointEvaluator.EvaluateManyAsync(_db,
            pts.Select(p => new AgendaHeader(p.Rid, p.MemberRids, p.AgendaDueDt, p.AgendaStatus)).ToList(),
            DateOnly.FromDateTime(DateTime.Today));

        // Every active officer in scope (a dept login sees its own officers; an officer login just
        // itself; an admin sees all). Listed even with no points so the dashboard mirrors departments.
        var officerQuery = _db.TblOfficers.Where(o => o.Active == "Y");
        if (scopeRids is not null) officerQuery = officerQuery.Where(o => scopeRids.Contains(o.Rid));
        var officers = await officerQuery
            .Select(o => new { o.Rid, o.OfficerName, Designation = o.Desig!.DesigName, DepartmentName = o.Dept.DepartmentName })
            .ToListAsync();
        var officerIds = officers.Select(o => o.Rid).ToHashSet();

        var acc = new Dictionary<int, (int Total, int Completed, int InProgress, int OverDue)>();
        var perOfficerMeeting = new Dictionary<(int Officer, int Meeting), (int Total, int Completed, int InProgress, int OverDue)>();
        foreach (var p in pts)
        {
            var status = evalMap[p.Rid].Status;
            bool completed = status == "Completed";
            bool overdue = status == "OverDue";
            foreach (var rid in ParseRids(p.MemberRids).Where(officerIds.Contains).Distinct())
            {
                acc.TryGetValue(rid, out var cur);
                cur.Total++;
                if (completed) cur.Completed++;
                else if (overdue) cur.OverDue++;
                else cur.InProgress++;
                acc[rid] = cur;

                var key = (rid, p.MeetingRid);
                perOfficerMeeting.TryGetValue(key, out var mc);
                mc.Total++;
                if (completed) mc.Completed++;
                else if (overdue) mc.OverDue++;
                else mc.InProgress++;
                perOfficerMeeting[key] = mc;
            }
        }

        var avgScore = perOfficerMeeting
            .GroupBy(kv => kv.Key.Officer)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(kv =>
                    MeetingScoreHelper.Compute(kv.Value.Total, kv.Value.Completed, kv.Value.InProgress, kv.Value.OverDue)), 1));

        return officers
            .Select(o =>
            {
                acc.TryGetValue(o.Rid, out var c);
                return new OfficerStatDto(
                    o.Rid, o.OfficerName, o.Designation, o.DepartmentName, c.Total,
                    c.Completed, c.InProgress, c.OverDue,
                    c.Total == 0 ? 0 : (int)Math.Round(100.0 * c.Completed / c.Total),
                    avgScore.GetValueOrDefault(o.Rid));
            })
            .OrderByDescending(d => d.CompletionRate)
            .ThenByDescending(d => d.TotalPoints)
            .ToList();
    }

    private static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }
    private static int? DeptForOfficer(string? departmentIds, int officerId)
    {
        if (string.IsNullOrWhiteSpace(departmentIds)) return null;
        foreach (var part in departmentIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bits = part.Split(':');
            if (bits.Length >= 2
                && int.TryParse(bits[0], out var rid) && rid == officerId
                && int.TryParse(bits[1], out var dept))
            {
                return dept;
            }
        }
        return null;
    }

    private static DashboardPointDto ToPoint(AgendaRow r, DateOnly today, string status,
        IReadOnlyDictionary<int, OfficerContactDto> contacts) => new(
        r.Rid, r.MeetingRid, r.MeetingSubject ?? "", r.MeetingDate, r.MeetingAgenda, r.AgendaMembers,
        r.DistrictName, r.AgendaDueDt,
        r.AgendaDueDt is null ? 0 : today.DayNumber - r.AgendaDueDt.Value.DayNumber,
        status, OfficerContacts.Resolve(r.MemberRids, r.AgendaMembers, contacts));
}
