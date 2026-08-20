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

    private record AgendaRow(long Rid, int MeetingRid, string? MeetingSubject, DateTime MeetingDate,
        string MeetingAgenda, string? AgendaMembers, string? MemberRids, string DistrictName, DateOnly? AgendaDueDt, string AgendaStatus);

    private async Task<List<AgendaRow>> LoadAsync(long? groupId, ScopeRequest? scope)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        var query =
            from a in _db.TbMeetingAgendas
            join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
            where a.Active == "Y" && m.Active == "Y"
            select new AgendaRow(a.Rid, a.MeetingRid, m.MeetingSubject, m.MeetingDate,
                a.MeetingAgenda, a.AgendaMembers, a.MemberRids, a.DistrictName, a.AgendaDueDt, a.AgendaStatus);

        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g => g.GroupRid == groupId && g.Active == "Y")
                .Select(g => g.MeetingRid).ToListAsync();
            query = query.Where(r => meetingIds.Contains(r.MeetingRid));
        }

        var rows = await query.ToListAsync();

        // A scoped login (department or officer) only counts action points involving its own officer(s).
        if (scopeRids is not null)
            rows = rows.Where(r => ParseRids(r.MemberRids).Any(scopeRids.Contains)).ToList();
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
    private async Task<int> CountMeetingsAsync(long? groupId, ScopeRequest? scope)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        var query = _db.TbMeetingSchedules.Where(m => m.Active == "Y");
        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g => g.GroupRid == groupId && g.Active == "Y")
                .Select(g => g.MeetingRid).ToListAsync();
            query = query.Where(m => meetingIds.Contains(m.Rid));
        }
        if (scopeRids is not null)
            query = query.Where(m => _db.TbMeetingMembers.Any(mm => mm.MeetingRid == m.Rid && scopeRids.Contains(mm.MemberRid)));
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

    public async Task<List<MeetingAbstractDto>> GetMeetingAbstractAsync(long? groupId = null, ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        // Pull each active point with its stored status flag so the per-meeting counts and Score use
        // the SAME completion signal as the Meetings screen, the per-meeting action-points screen and
        // Reports (AgendaStatusHelper: the stored flag + due date, not raw ATR progress — legacy
        // backfilled points are Completed with no progress recorded).
        var query =
            from a in _db.TbMeetingAgendas
            join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
            where a.Active == "Y" && m.Active == "Y"
            select new
            {
                a.Rid, a.MeetingRid, m.MeetingSubject, m.MeetingDate, a.MemberRids, a.AgendaDueDt, a.AgendaStatus
            };

        if (groupId is not null)
        {
            var meetingIds = await _db.TbMeetingMappedGroups
                .Where(g => g.GroupRid == groupId && g.Active == "Y")
                .Select(g => g.MeetingRid).ToListAsync();
            query = query.Where(r => meetingIds.Contains(r.MeetingRid));
        }

        var rows = await query.ToListAsync();

        // A scoped login (department or officer) counts only the points involving its own officer(s).
        if (scopeRids is not null)
            rows = rows.Where(r => ParseRids(r.MemberRids).Any(scopeRids.Contains)).ToList();

        var evalMap = await PointEvaluator.EvaluateManyAsync(_db,
            rows.Select(r => new AgendaHeader(r.Rid, r.MemberRids, r.AgendaDueDt, r.AgendaStatus)).ToList(),
            DateOnly.FromDateTime(DateTime.Today));

        return rows
            .GroupBy(r => new { r.MeetingRid, r.MeetingSubject, r.MeetingDate })
            .Select(g =>
            {
                int total = g.Count(), completed = 0, overdue = 0, inProgress = 0;
                int od0To7 = 0, od8To30 = 0, od31To60 = 0, od60Plus = 0;
                var todayNum = DateOnly.FromDateTime(DateTime.Today).DayNumber;
                foreach (var r in g)
                {
                    switch (evalMap[r.Rid].Status)
                    {
                        case "Completed": completed++; break;
                        case "OverDue":
                            overdue++;
                            // OverDue implies a due date in the past, so bucket by days elapsed since it.
                            var days = r.AgendaDueDt is DateOnly due ? todayNum - due.DayNumber : 0;
                            if (days <= 7) od0To7++;
                            else if (days <= 30) od8To30++;
                            else if (days <= 60) od31To60++;
                            else od60Plus++;
                            break;
                        default: inProgress++; break;
                    }
                }
                return new MeetingAbstractDto(
                    g.Key.MeetingRid, g.Key.MeetingSubject, g.Key.MeetingDate,
                    total, completed, inProgress, overdue,
                    od0To7, od8To30, od31To60, od60Plus,
                    MeetingScoreHelper.Compute(total, completed, inProgress, overdue));
            })
            .OrderByDescending(a => a.MeetingDate)
            .ToList();
    }

    // Per-department performance: every action point is credited to each department that has an
    // officer responsible for it (a point spanning two departments counts once for each). The
    // completion rate drives the top/worst-performing ranking on the dashboard.
    // Per-department performance for the admin Department Dashboard. Every number in a row — the
    // Total/Completed/InProgress/OverDue counts, the CompletionRate, and the accumulated Score — is
    // derived from the SAME completion signal the whole app uses (AgendaStatusHelper: the stored
    // status flag + due date, not raw ATR progress which is blank on legacy backfilled points). The
    // accumulated score is the average of the department's per-meeting scores. A point spanning two
    // departments is credited to each.
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

    private static DashboardPointDto ToPoint(AgendaRow r, DateOnly today, string status,
        IReadOnlyDictionary<int, OfficerContactDto> contacts) => new(
        r.Rid, r.MeetingRid, r.MeetingSubject ?? "", r.MeetingDate, r.MeetingAgenda, r.AgendaMembers,
        r.DistrictName, r.AgendaDueDt,
        r.AgendaDueDt is null ? 0 : today.DayNumber - r.AgendaDueDt.Value.DayNumber,
        status, OfficerContacts.Resolve(r.MemberRids, r.AgendaMembers, contacts));
}
