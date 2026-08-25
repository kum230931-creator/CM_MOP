using System.Globalization;
using System.Text;
using CMOmeets.Application.Agendas;
using CMOmeets.Application.Common;
using CMOmeets.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Reports;

public record MeetingsReportRow(int MeetingRid, DateTime MeetingDate, string MeetingPlace,
    string? MeetingSubject, int MemberCount, int TotalPoints, int Completed, int InProgress, int OverDue);

public class ReportsService
{
    private readonly CmoMeetsDbContext _db;
    public ReportsService(CmoMeetsDbContext db) => _db = db;

    public async Task<List<MeetingsReportRow>> GetMeetingsReportAsync(
    DateTime? from,
    DateTime? to,
    ScopeRequest? scope = null)
    {
        // Resolve scope first.
        // For a normal officer, we override the resolved department-based
        // RIDs with ONLY the logged-in officer's own RID.
        //
        // Nodal / CMO continues to use the existing department-scope logic.
        List<int>? scopeOfficerRids =
            await ScopeResolver.ResolveRidsAsync(_db, scope);

        if (scope?.IsOfficer == true && scope.OfficerLoginId is int officerId)
        {
            // Normal officer:
            // Only this officer's meetings should be visible.
            scopeOfficerRids = new List<int> { officerId };
        }

        // Resolve meetings belonging to the scoped officer(s).
        List<int>? scopeMeetingIds = null;

        if (scopeOfficerRids is not null)
        {
            scopeMeetingIds = await _db.TbMeetingMembers
                .Where(mm =>
                    scopeOfficerRids.Contains(mm.MemberRid) &&
                    mm.MemberRid != 0 &&
                    mm.DesignationId != 0)
                .Select(mm => mm.MeetingRid)
                .Distinct()
                .ToListAsync();
        }

        // Get active meetings within the requested date range.
        var meetingsQuery = _db.TbMeetingSchedules
            .Where(m =>
                m.Active == "Y" &&
                (from == null || m.MeetingDate >= from) &&
                (to == null || m.MeetingDate <= to));

        if (scopeMeetingIds is not null)
        {
            meetingsQuery = meetingsQuery
                .Where(m => scopeMeetingIds.Contains(m.Rid));
        }

        var meetings = await meetingsQuery
            .OrderByDescending(m => m.MeetingDate)
            .Select(m => new
            {
                m.Rid,
                m.MeetingDate,
                m.MeetingPlace,
                m.MeetingSubject
            })
            .ToListAsync();

        var meetingIds = meetings
            .Select(m => m.Rid)
            .ToList();

        if (meetingIds.Count == 0)
            return new List<MeetingsReportRow>();

        // Get active agendas/action points for these meetings.
        var agendas = await _db.TbMeetingAgendas
            .Where(a =>
                a.Active == "Y" &&
                meetingIds.Contains(a.MeetingRid))
            .Select(a => new
            {
                a.Rid,
                a.MeetingRid,
                a.MemberRids,
                a.AgendaStatus,
                a.AgendaDueDt
            })
            .ToListAsync();

        // Get valid meeting members only.
        //
        // MemberRid = 0 OR DesignationId = 0 means the officer is no longer
        // validly assigned to that meeting (for example, after transfer).
        var members = await _db.TbMeetingMembers
            .Where(mm =>
                meetingIds.Contains(mm.MeetingRid) &&
                mm.MemberRid != 0 &&
                mm.DesignationId != 0)
            .Select(mm => new
            {
                mm.MeetingRid,
                mm.MemberRid
            })
            .ToListAsync();

        // For a scoped login, only count its own action points and members.
        //
        // Normal officer:
        //     scopeOfficerRids = [current officer RID]
        //
        // Nodal / CMO:
        //     scopeOfficerRids = all officer RIDs in the department scope.
        if (scopeOfficerRids is not null)
        {
            agendas = agendas
                .Where(a =>
                    ParseRids(a.MemberRids)
                        .Any(scopeOfficerRids.Contains))
                .ToList();

            members = members
                .Where(mm =>
                    scopeOfficerRids.Contains(mm.MemberRid))
                .ToList();
        }

        var memberCounts = members
            .GroupBy(mm => mm.MeetingRid)
            .Select(g => new
            {
                MeetingRid = g.Key,
                Count = g.Count()
            })
            .ToList();

        var evalMap = await PointEvaluator.EvaluateManyAsync(
            _db,
            agendas
                .Select(a => new AgendaHeader(
                    a.Rid,
                    a.MemberRids,
                    a.AgendaDueDt,
                    a.AgendaStatus))
                .ToList(),
            DateOnly.FromDateTime(DateTime.Today));

        return meetings
            .Select(m =>
            {
                var pts = agendas
                    .Where(a => a.MeetingRid == m.Rid)
                    .Select(a => evalMap[a.Rid].Status)
                    .ToList();

                return new MeetingsReportRow(
                    m.Rid,
                    m.MeetingDate,
                    m.MeetingPlace,
                    m.MeetingSubject,
                    memberCounts
                        .FirstOrDefault(c => c.MeetingRid == m.Rid)
                        ?.Count ?? 0,
                    pts.Count,
                    pts.Count(s => s == "Completed"),
                    pts.Count(s => s == "InProgress"),
                    pts.Count(s => s == "OverDue"));
            })
            .ToList();
    }
    //public async Task<List<MeetingsReportRow>> GetMeetingsReportAsync(DateTime? from, DateTime? to, ScopeRequest? scope = null)
    //{
    //    // For a scoped login (officer/nodal), restrict the whole report to its own data.
    //    List<int>? scopeOfficerRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
    //    List<int>? scopeMeetingIds = null;
    //    if (scopeOfficerRids is not null)
    //        scopeMeetingIds = await _db.TbMeetingMembers
    //            .Where(mm => scopeOfficerRids.Contains(mm.MemberRid))
    //            .Select(mm => mm.MeetingRid).Distinct().ToListAsync();

    //    var meetingsQuery = _db.TbMeetingSchedules
    //        .Where(m => m.Active == "Y"
    //            && (from == null || m.MeetingDate >= from)
    //            && (to == null || m.MeetingDate <= to));
    //    if (scopeMeetingIds is not null)
    //        meetingsQuery = meetingsQuery.Where(m => scopeMeetingIds.Contains(m.Rid));

    //    var meetings = await meetingsQuery
    //        .OrderByDescending(m => m.MeetingDate)
    //        .Select(m => new { m.Rid, m.MeetingDate, m.MeetingPlace, m.MeetingSubject })
    //        .ToListAsync();

    //    var meetingIds = meetings.Select(m => m.Rid).ToList();

    //    var agendas = await _db.TbMeetingAgendas
    //        .Where(a => a.Active == "Y" && meetingIds.Contains(a.MeetingRid))
    //        .Select(a => new { a.Rid, a.MeetingRid, a.MemberRids, a.AgendaStatus, a.AgendaDueDt })
    //        .ToListAsync();

    //    var members = await _db.TbMeetingMembers
    //        .Where(mm => meetingIds.Contains(mm.MeetingRid))
    //        .Select(mm => new { mm.MeetingRid, mm.MemberRid })
    //        .ToListAsync();

    //    // A scoped login (department or officer) only counts its own action points and members.
    //    if (scopeOfficerRids is not null)
    //    {
    //        agendas = agendas.Where(a => ParseRids(a.MemberRids).Any(scopeOfficerRids.Contains)).ToList();
    //        members = members.Where(mm => scopeOfficerRids.Contains(mm.MemberRid)).ToList();
    //    }

    //    var memberCounts = members
    //        .GroupBy(mm => mm.MeetingRid)
    //        .Select(g => new { MeetingRid = g.Key, Count = g.Count() })
    //        .ToList();

    //    var evalMap = await PointEvaluator.EvaluateManyAsync(_db,
    //        agendas.Select(a => new AgendaHeader(a.Rid, a.MemberRids, a.AgendaDueDt, a.AgendaStatus)).ToList(),
    //        DateOnly.FromDateTime(DateTime.Today));

    //    return meetings.Select(m =>
    //    {
    //        var pts = agendas.Where(a => a.MeetingRid == m.Rid)
    //            .Select(a => evalMap[a.Rid].Status).ToList();
    //        return new MeetingsReportRow(
    //            m.Rid, m.MeetingDate, m.MeetingPlace, m.MeetingSubject,
    //            memberCounts.FirstOrDefault(c => c.MeetingRid == m.Rid)?.Count ?? 0,
    //            pts.Count, pts.Count(s => s == "Completed"), pts.Count(s => s == "InProgress"),
    //            pts.Count(s => s == "OverDue"));
    //    }).ToList();
    //}

    private static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }

    public static string ToCsv(IEnumerable<ActionablePointDto> points)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Meeting,Meeting Date,Place,Agenda Point,Responsible,District,Due Date,Status,Days Overdue,Remarks");
        foreach (var p in points)
        {
            string[] cells =
            {
                p.MeetingSubject, p.MeetingDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                p.MeetingPlace, p.MeetingAgenda, ResponsibleWithNumbers(p), p.DistrictName,
                p.AgendaDueDt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "",
                p.Status, p.Status == "OverDue" ? p.DueDays.ToString() : "", p.RemarksCount.ToString()
            };
            sb.AppendLine(string.Join(",", cells.Select(Escape)));
        }
        return sb.ToString();
    }

    // Concerned officers as "Name (mobile)", matching how the grids show them on screen.
    private static string ResponsibleWithNumbers(ActionablePointDto p) =>
        p.Members.Count == 0
            ? p.AgendaMembers ?? ""
            : string.Join("; ", p.Members.Select(m =>
                string.IsNullOrWhiteSpace(m.Mobile) ? m.Name : $"{m.Name} ({m.Mobile})"));

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
