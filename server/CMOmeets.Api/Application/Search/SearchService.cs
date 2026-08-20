using CMOmeets.Application.Agendas;
using CMOmeets.Application.Common;
using CMOmeets.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Search;

/// One hit in the global ("deep") search. Type drives the icon + which page the client opens;
/// Id is the entity's rid, Label is the primary text, Sublabel the muted secondary line.
public record SearchHit(string Type, long Id, string Label, string? Sublabel);

public record SearchResults(
    List<SearchHit> Meetings, List<SearchHit> ActionPoints, List<SearchHit> Officers, List<SearchHit> Departments)
{
    public int Total => Meetings.Count + ActionPoints.Count + Officers.Count + Departments.Count;
}

/// Global search across the main entities. Scope-aware: admin/cm see everything; a nodal/officer login
/// only matches within its own data (its meetings, its action points, its officers/departments).
public class SearchService
{
    private readonly CmoMeetsDbContext _db;
    public SearchService(CmoMeetsDbContext db) => _db = db;

    public async Task<SearchResults> SearchAsync(string? query, ScopeRequest? scope, int perType = 8)
    {
        var q = (query ?? string.Empty).Trim();
        var empty = new SearchResults(new(), new(), new(), new());
        if (q.Length < 2) return empty;

        var like = $"%{Escape(q)}%";
        // null => admin/cm (unscoped); otherwise the set of officer rids this login may see.
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);

        // ---- Meetings (subject / place) ----
        var meetingsQ = _db.TbMeetingSchedules.Where(m => m.Active == "Y"
            && (EF.Functions.Like(m.MeetingSubject, like) || EF.Functions.Like(m.MeetingPlace ?? "", like)));
        if (scopeRids is not null)
            meetingsQ = meetingsQ.Where(m =>
                _db.TbMeetingMembers.Any(mm => mm.MeetingRid == m.Rid && scopeRids.Contains(mm.MemberRid)));
        var meetings = (await meetingsQ
                .OrderByDescending(m => m.MeetingDate)
                .Select(m => new { m.Rid, m.MeetingSubject, m.MeetingPlace, m.MeetingDate })
                .Take(perType).ToListAsync())
            .Select(m => new SearchHit("meeting", m.Rid, m.MeetingSubject ?? "(untitled meeting)",
                $"{m.MeetingDate:dd MMM yyyy}{(string.IsNullOrWhiteSpace(m.MeetingPlace) ? "" : " · " + m.MeetingPlace)}"))
            .ToList();

        // ---- Action points (point text / its meeting's subject) ----
        // MemberRids is a CSV, so the scope match is done in memory after a coarse text filter.
        var pointRows = await (
            from a in _db.TbMeetingAgendas
            join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
            where a.Active == "Y"
                && (EF.Functions.Like(a.MeetingAgenda, like) || EF.Functions.Like(m.MeetingSubject, like))
            orderby m.MeetingDate descending
            select new { a.Rid, a.MeetingAgenda, a.MemberRids, m.MeetingSubject, m.MeetingDate })
            .Take(perType * 4).ToListAsync();
        var points = pointRows
            .Where(p => scopeRids is null || PointEvaluator.ParseRids(p.MemberRids).Any(scopeRids.Contains))
            .Take(perType)
            .Select(p => new SearchHit("actionpoint", p.Rid, Trim(p.MeetingAgenda),
                $"{p.MeetingSubject} · {p.MeetingDate:dd MMM yyyy}"))
            .ToList();

        // ---- Officers (name) ----
        var officersQ = _db.TblOfficers.Where(o => o.Active == "Y" && EF.Functions.Like(o.OfficerName, like));
        if (scopeRids is not null) officersQ = officersQ.Where(o => scopeRids.Contains(o.Rid));
        var officers = (await officersQ
                .OrderBy(o => o.OfficerName)
                .Select(o => new { o.Rid, o.OfficerName, Desig = o.Desig!.DesigName, Dept = o.Dept.DepartmentName })
                .Take(perType).ToListAsync())
            .Select(o => new SearchHit("officer", o.Rid, o.OfficerName,
                string.Join(" · ", new[] { o.Desig, o.Dept }.Where(s => !string.IsNullOrWhiteSpace(s)))))
            .ToList();

        // ---- Departments (name) ----
        var deptsQ = _db.DepartmentMas.Where(d => EF.Functions.Like(d.DepartmentName, like));
        if (scopeRids is not null)
        {
            // Every department the in-scope officers serve — a flat set per officer, so take the
            // junction rows and fall back to the legacy DeptId only where there are none.
            var scoped = await _db.TblOfficers.Where(o => scopeRids.Contains(o.Rid))
                .Select(o => new { o.DeptId, JunctionDeptIds = o.OfficerDepartments.Where(x => x.Active == "Y").Select(x => x.DeptId).ToList() })
                .ToListAsync();
            var scopeDeptIds = scoped
                .SelectMany(o => o.JunctionDeptIds.Count > 0 ? o.JunctionDeptIds : new List<int> { o.DeptId })
                .Distinct().ToList();
            deptsQ = deptsQ.Where(d => scopeDeptIds.Contains(d.Rid));
        }
        var departments = (await deptsQ
                .OrderBy(d => d.DepartmentName)
                .Select(d => new { d.Rid, d.DepartmentName, Ministry = d.Ministry!.MinistryName })
                .Take(perType).ToListAsync())
            .Select(d => new SearchHit("department", d.Rid, d.DepartmentName, d.Ministry))
            .ToList();

        return new SearchResults(meetings, points, officers, departments);
    }

    // Collapse whitespace and cap long point text so a result row stays a single line.
    private static string Trim(string? s)
    {
        s = (s ?? string.Empty).Replace('\n', ' ').Replace('\r', ' ').Trim();
        return s.Length > 120 ? s[..120] + "…" : s;
    }

    // Escape LIKE wildcards in the user's query so '%'/'_' are treated literally.
    private static string Escape(string s) => s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
