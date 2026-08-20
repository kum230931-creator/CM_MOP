using CMOmeets.Application.Common;
using CMOmeets.Domain.Data;
using CMOmeets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Agendas;

public class AgendasService
{
    private readonly CmoMeetsDbContext _db;
    public AgendasService(CmoMeetsDbContext db) => _db = db;

    public async Task<List<AgendaDto>> GetAgendasByMeetingAsync(int meetingId, ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        if (scopeRids is not null && !await MeetingHasMemberAsync(meetingId, scopeRids))
            return new List<AgendaDto>();

        var rows = await _db.TbMeetingAgendas
            .Where(a => a.MeetingRid == meetingId && a.Active == "Y")
            .OrderBy(a => a.Rid)
            .Select(a => new
            {
                a.Rid, a.MeetingRid, a.MeetingAgenda, a.AgendaMembers, a.MemberRids,
                a.AgendaDueDt, a.DistrictName, a.AgendaStatus, a.AddedAt,
                a.IsOfficerCalled, a.OfficerRemark,
                RemarksCount = _db.TbRemarksOnAgendas.Count(r => r.AgendaRid == a.Rid),
                // Whether a concerned department has opened (expanded) this point, and when / by whom.
                Opened = _db.TbActionPointViews.Any(v => v.AgendaRid == a.Rid),
                FirstViewedAt = _db.TbActionPointViews
                    .Where(v => v.AgendaRid == a.Rid).Min(v => (DateTime?)v.FirstViewedAt),
                LastViewedAt = _db.TbActionPointViews
                    .Where(v => v.AgendaRid == a.Rid).Max(v => (DateTime?)v.LastViewedAt),
                ViewedBy = _db.TbActionPointViews
                    .Where(v => v.AgendaRid == a.Rid)
                    .OrderByDescending(v => v.LastViewedAt)
                    .Select(v => v.ViewedBy)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // A scoped login (department or officer) only sees action points that involve one of its
        // own officer rids (the whole department's officers, or the single officer respectively).
        if (scopeRids is not null)
            rows = rows.Where(a => ParseRids(a.MemberRids).Any(scopeRids.Contains)).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var evalMap = await PointEvaluator.EvaluateManyAsync(_db,
            rows.Select(a => new AgendaHeader(a.Rid, a.MemberRids, a.AgendaDueDt, a.AgendaStatus)).ToList(), today);
        var contacts = await OfficerContacts.LoadAsync(_db, rows.Select(a => a.MemberRids));
        return rows.Select(a =>
        {
            var (status, progress) = evalMap[a.Rid];
            return new AgendaDto(
                a.Rid, a.MeetingRid, a.MeetingAgenda, a.AgendaMembers, a.MemberRids,
                a.AgendaDueDt, a.DistrictName, status,
                a.RemarksCount, progress, a.AddedAt,
                a.Opened, a.FirstViewedAt, a.LastViewedAt, a.ViewedBy,
                OfficerContacts.Resolve(a.MemberRids, a.AgendaMembers, contacts),
                a.IsOfficerCalled, a.OfficerRemark);
        }).ToList();
    }

    // Single action point by id, scope-checked — drives the action-point detail page.
    public async Task<AgendaDto?> GetAgendaAsync(long id, ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        var a = await _db.TbMeetingAgendas
            .Where(x => x.Rid == id && x.Active == "Y")
            .Select(x => new
            {
                x.Rid, x.MeetingRid, x.MeetingAgenda, x.AgendaMembers, x.MemberRids,
                x.AgendaDueDt, x.DistrictName, x.AgendaStatus, x.AddedAt,
                x.IsOfficerCalled, x.OfficerRemark,
                RemarksCount = _db.TbRemarksOnAgendas.Count(r => r.AgendaRid == x.Rid),
                Opened = _db.TbActionPointViews.Any(v => v.AgendaRid == x.Rid),
                FirstViewedAt = _db.TbActionPointViews
                    .Where(v => v.AgendaRid == x.Rid).Min(v => (DateTime?)v.FirstViewedAt),
                LastViewedAt = _db.TbActionPointViews
                    .Where(v => v.AgendaRid == x.Rid).Max(v => (DateTime?)v.LastViewedAt),
                ViewedBy = _db.TbActionPointViews
                    .Where(v => v.AgendaRid == x.Rid)
                    .OrderByDescending(v => v.LastViewedAt)
                    .Select(v => v.ViewedBy)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
        if (a is null) return null;
        // A scoped login (dept/officer) may only open action points involving one of its own officers.
        if (scopeRids is not null && !ParseRids(a.MemberRids).Any(scopeRids.Contains)) return null;
        var atrs = await _db.TbRemarksOnAgendas
            .Where(r => r.AgendaRid == a.Rid)
            .Select(r => new AtrRow(r.MemberRid, r.Rid, r.ProgressPercentage, r.RemarkStatus))
            .ToListAsync();
        var (status, progress) = PointEvaluator.Evaluate(
            PointEvaluator.ParseRids(a.MemberRids).ToList(), atrs, a.AgendaDueDt, a.AgendaStatus,
            DateOnly.FromDateTime(DateTime.Today));
        var contacts = await OfficerContacts.LoadAsync(_db, new[] { a.MemberRids });
        return new AgendaDto(
            a.Rid, a.MeetingRid, a.MeetingAgenda, a.AgendaMembers, a.MemberRids,
            a.AgendaDueDt, a.DistrictName, status,
            a.RemarksCount, progress, a.AddedAt,
            a.Opened, a.FirstViewedAt, a.LastViewedAt, a.ViewedBy,
            OfficerContacts.Resolve(a.MemberRids, a.AgendaMembers, contacts),
            a.IsOfficerCalled, a.OfficerRemark);
    }

    public async Task<AgendaDto> CreateAgendaAsync(AgendaSaveDto dto, string actor, ScopeRequest? scope = null)
    {
        // A scoped login may only create points in a meeting it is entitled to: an officer in a meeting
        // involving one of its departments, a nodal in a meeting of its department; admin is unscoped.
        // (Create is restricted to admin/officer/nodal at the controller.)
        await EnsureScopeAccessAsync(dto.MeetingRid, await ScopeResolver.ResolveRidsAsync(_db, scope));
        await ValidateDueDateAsync(dto.MeetingRid, dto.AgendaDueDt);
        var (names, rids) = await BuildMemberCsvAsync(dto.MemberOfficerIds);
        var entity = new TbMeetingAgenda
        {
            MeetingRid = dto.MeetingRid,
            MeetingAgenda = dto.MeetingAgenda.Trim(),
            AgendaMembers = names,
            MemberRids = rids,
            AgendaDueDt = dto.AgendaDueDt,
            DistrictName = string.IsNullOrWhiteSpace(dto.DistrictName) ? "Headquarters" : dto.DistrictName,
            AgendaStatus = "InProgress",
            Active = "Y",
            AddedAt = DateTime.Now,
            AddedBy = actor
        };
        _db.TbMeetingAgendas.Add(entity);
        await _db.SaveChangesAsync();

        var contacts = await OfficerContacts.LoadAsync(_db, new[] { entity.MemberRids });
        return new AgendaDto(entity.Rid, entity.MeetingRid, entity.MeetingAgenda, entity.AgendaMembers,
            entity.MemberRids, entity.AgendaDueDt, entity.DistrictName,
            AgendaStatusHelper.Derive(entity.AgendaStatus, entity.AgendaDueDt), 0, null, entity.AddedAt,
            false, null, null, null,
            OfficerContacts.Resolve(entity.MemberRids, entity.AgendaMembers, contacts),
            entity.IsOfficerCalled, entity.OfficerRemark);
    }

    public async Task<bool> UpdateAgendaAsync(long id, AgendaSaveDto dto, ScopeRequest? scope = null)
    {
        var entity = await _db.TbMeetingAgendas.FindAsync(id);
        if (entity is null) return false;
        await EnsureScopeAccessAsync(entity.MeetingRid, await ScopeResolver.ResolveRidsAsync(_db, scope));
        await ValidateDueDateAsync(entity.MeetingRid, dto.AgendaDueDt);
        var (names, rids) = await BuildMemberCsvAsync(dto.MemberOfficerIds);
        entity.MeetingAgenda = dto.MeetingAgenda.Trim();
        entity.AgendaMembers = names;
        entity.MemberRids = rids;
        entity.AgendaDueDt = dto.AgendaDueDt;
        entity.DistrictName = string.IsNullOrWhiteSpace(dto.DistrictName) ? "Headquarters" : dto.DistrictName;
        await _db.SaveChangesAsync();
        return true;
    }

    // Admin-only follow-up on the concerned officer. Deliberately unscoped and separate from
    // UpdateAgendaAsync: it is called straight from the grids and must not disturb the point's
    // members or due date. Clearing the flag drops the remark with it, so a point never shows a
    // stale note against "No".
    public async Task<bool> SetOfficerCallAsync(long id, OfficerCallSaveDto dto)
    {
        var entity = await _db.TbMeetingAgendas.FindAsync(id);
        if (entity is null || entity.Active != "Y") return false;
        entity.IsOfficerCalled = dto.IsOfficerCalled;
        entity.OfficerRemark = dto.IsOfficerCalled
            ? (string.IsNullOrWhiteSpace(dto.OfficerRemark) ? null : dto.OfficerRemark.Trim())
            : null;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAgendaAsync(long id, ScopeRequest? scope = null)
    {
        var entity = await _db.TbMeetingAgendas.FindAsync(id);
        if (entity is null) return false;
        await EnsureScopeAccessAsync(entity.MeetingRid, await ScopeResolver.ResolveRidsAsync(_db, scope));
        entity.Active = "N";
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Remarks / ATR ----------
    public async Task<List<RemarkDto>> GetRemarksAsync(long agendaId) =>
        await _db.TbRemarksOnAgendas
            .Where(r => r.AgendaRid == agendaId)
            .OrderBy(r => r.Rid)
            .Select(r => new RemarkDto(r.Rid, r.AgendaRid, r.MemberRid, r.MemberName, r.AgendaRemarks,
                r.RemarksDate, r.RemarkStatus, r.ProgressPercentage, r.AtrDoc, r.AddedAt,
                r.ExplanationRequest, r.ExplanationRequestedAt,
                r.ExplanationText, r.ExplanationDoc, r.ExplainedAt))
            .ToListAsync();

    // Derived activity timeline for a single action point — built from existing timestamps
    // (no separate audit table). Covers the whole lifecycle: creation, department opens,
    // each ATR, and every explanation request/reply, ordered chronologically.
    public async Task<List<ActivityLogEntryDto>> GetActivityLogAsync(long agendaId)
    {
        var agenda = await _db.TbMeetingAgendas
            .Where(a => a.Rid == agendaId)
            .Select(a => new { a.AddedAt, a.AddedBy })
            .FirstOrDefaultAsync();
        if (agenda is null) return new List<ActivityLogEntryDto>();

        var events = new List<ActivityLogEntryDto>();

        // 1. Creation of the action point.
        if (agenda.AddedAt is DateTime created)
            events.Add(new ActivityLogEntryDto($"Action point created{By(agenda.AddedBy)}", created));

        // 2. Concerned department(s) opening the point (one row per department).
        var views = await _db.TbActionPointViews
            .Where(v => v.AgendaRid == agendaId)
            .Select(v => new { v.FirstViewedAt, v.ViewedBy })
            .ToListAsync();
        foreach (var v in views)
            events.Add(new ActivityLogEntryDto($"Opened by {v.ViewedBy}", v.FirstViewedAt));

        // 3. Each ATR, plus its explanation request/reply if any.
        var remarks = await _db.TbRemarksOnAgendas
            .Where(r => r.AgendaRid == agendaId)
            .Select(r => new
            {
                r.AddedAt, r.MemberName, r.RemarkStatus,
                r.ExplanationRequestedBy, r.ExplanationRequestedAt,
                r.ExplainedBy, r.ExplainedAt
            })
            .ToListAsync();
        foreach (var r in remarks)
        {
            if (r.AddedAt is DateTime atrAt)
            {
                var status = string.IsNullOrWhiteSpace(r.RemarkStatus) ? "" : $" ({HumanizeStatus(r.RemarkStatus)})";
                events.Add(new ActivityLogEntryDto($"ATR submitted by {r.MemberName}{status}", atrAt));
            }
            if (r.ExplanationRequestedAt is DateTime reqAt)
                events.Add(new ActivityLogEntryDto($"Explanation requested{By(r.ExplanationRequestedBy)}", reqAt));
            if (r.ExplainedAt is DateTime expAt)
                events.Add(new ActivityLogEntryDto($"Explanation provided{By(r.ExplainedBy)}", expAt));
        }

        return events.OrderBy(e => e.When).ToList();
    }

    private static string By(string? actor) => string.IsNullOrWhiteSpace(actor) ? "" : $" by {actor}";

    private static string HumanizeStatus(string status) => status switch
    {
        "InProgress" => "In Progress",
        "OverDue" => "Over Due",
        _ => status
    };

    public async Task<RemarkDto?> AddRemarkAsync(RemarkSaveDto dto, string actor, ScopeRequest? scope = null)
    {
        var agenda = await _db.TbMeetingAgendas.FindAsync(dto.AgendaRid);
        if (agenda is null) return null;
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        await EnsureScopeAccessAsync(agenda.MeetingRid, scopeRids);

        // A scoped login (officer / nodal) may only report on action points that fall within its
        // scope — i.e. a responsible officer of the point is one of its own officers.
        if (scopeRids is not null && !ParseRids(agenda.MemberRids).Any(scopeRids.Contains))
            throw new UnauthorizedAccessException("You can only add ATRs to action points within your department(s).");

        // The point's responsible officers, and every ATR so far (per-officer, not just the latest).
        var officerRids = ParseRids(agenda.MemberRids).ToList();
        var existingAtrs = await _db.TbRemarksOnAgendas
            .Where(r => r.AgendaRid == dto.AgendaRid)
            .Select(r => new AtrRow(r.MemberRid, r.Rid, r.ProgressPercentage, r.RemarkStatus))
            .ToListAsync();

        // A point is closed to scoped (officer/nodal) logins only once EVERY responsible officer has
        // completed — one officer finishing must not lock the others out.
        if (scopeRids is not null && PointEvaluator.AllOfficersDone(officerRids, existingAtrs))
            throw new InvalidOperationException("This action point is completed; you can't add new ATRs.");

        // An in-progress ATR's revised due date can't be back-dated — a still-open action point
        // can't already be past due. A Completed ATR may carry a past due date (the work is done).
        if (!string.Equals(dto.RemarkStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            && dto.AgendaDueDt is DateOnly due && due < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException(
                "An in-progress action point's revised due date cannot be in the past.");

        var remark = new TbRemarksOnAgenda
        {
            MeetingRid = agenda.MeetingRid,
            AgendaRid = dto.AgendaRid,
            AgendaDueDate = dto.AgendaDueDt,
            MemberRid = dto.MemberRid,
            MemberName = dto.MemberName,
            AgendaRemarks = dto.AgendaRemarks.Trim(),
            RemarksDate = dto.RemarksDate ?? DateOnly.FromDateTime(DateTime.Today),
            RemarkStatus = dto.RemarkStatus,
            ProgressPercentage = NormalizeProgress(dto.RemarkStatus, dto.ProgressPercentage),
            AtrDoc = dto.AtrDoc,
            AddedAt = DateTime.Now,
            AddedBy = actor
        };
        _db.TbRemarksOnAgendas.Add(remark);
        if (dto.AgendaDueDt is not null) agenda.AgendaDueDt = dto.AgendaDueDt;

        // Recompute completion from ALL officers' latest ATRs (including this new one) so one officer
        // completing doesn't mark the whole point done. A point with no responsible officers keeps the
        // legacy "latest remark wins" behaviour. (Read paths compute this live too; the stored flag is
        // kept correct mainly for the no-ATR fallback and the closed-to-scoped-logins rule above.)
        var withNew = existingAtrs
            .Append(new AtrRow(remark.MemberRid, long.MaxValue, remark.ProgressPercentage, remark.RemarkStatus))
            .ToList();
        agenda.AgendaStatus = officerRids.Count == 0
            ? dto.RemarkStatus
            : (PointEvaluator.AllOfficersDone(officerRids, withNew) ? "Completed" : "InProgress");

        await _db.SaveChangesAsync();
        return new RemarkDto(remark.Rid, remark.AgendaRid, remark.MemberRid, remark.MemberName,
            remark.AgendaRemarks, remark.RemarksDate, remark.RemarkStatus, remark.ProgressPercentage, remark.AtrDoc, remark.AddedAt,
            remark.ExplanationRequest, remark.ExplanationRequestedAt,
            remark.ExplanationText, remark.ExplanationDoc, remark.ExplainedAt);
    }

    // ----- ATR explanation: admin asks, department replies -----
    public async Task<bool> RequestExplanationAsync(long remarkId, string request, string actor)
    {
        if (string.IsNullOrWhiteSpace(request))
            throw new InvalidOperationException("Please enter what the department should explain.");
        var remark = await _db.TbRemarksOnAgendas.FindAsync(remarkId);
        if (remark is null) return false;
        remark.ExplanationRequest = request.Trim();
        remark.ExplanationRequestedBy = actor;
        remark.ExplanationRequestedAt = DateTime.Now;
        // An admin asking for an explanation knocks the reported progress down by 10% (floored at 0)
        // and marks this ATR no longer "Completed", so a completed point it reopens stops counting this
        // officer as done under the per-officer evaluation (PointEvaluator).
        remark.ProgressPercentage = Math.Max(0, (remark.ProgressPercentage ?? 0) - 10);
        if (string.Equals(remark.RemarkStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            remark.RemarkStatus = "InProgress";
        // A fresh request clears any earlier reply.
        remark.ExplanationText = null;
        remark.ExplanationDoc = null;
        remark.ExplainedBy = null;
        remark.ExplainedAt = null;

        // Requesting an explanation reopens a completed action point — it's no longer "done"
        // until the department responds, so its status reverts from Completed to In Progress.
        var agenda = await _db.TbMeetingAgendas.FindAsync(remark.AgendaRid);
        if (agenda is not null && string.Equals(agenda.AgendaStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            agenda.AgendaStatus = "InProgress";

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitExplanationAsync(long remarkId, ExplanationReplyDto dto, string actor, ScopeRequest? scope = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Explanation))
            throw new InvalidOperationException("Please enter an explanation.");
        var remark = await _db.TbRemarksOnAgendas.FindAsync(remarkId);
        if (remark is null) return false;
        await EnsureScopeAccessAsync(remark.MeetingRid, await ScopeResolver.ResolveRidsAsync(_db, scope));
        if (string.IsNullOrWhiteSpace(remark.ExplanationRequest))
            throw new InvalidOperationException("No explanation has been requested for this ATR.");
        remark.ExplanationText = dto.Explanation.Trim();
        remark.ExplanationDoc = dto.Doc;
        remark.ExplainedBy = actor;
        remark.ExplainedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRemarkAsync(long id)
    {
        var entity = await _db.TbRemarksOnAgendas.FindAsync(id);
        if (entity is null) return false;
        _db.TbRemarksOnAgendas.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    // ----- Action-point view log (concerned dept opening a point) -----
    public async Task<bool> RecordViewAsync(long agendaId, int deptId, string actor)
    {
        var agenda = await _db.TbMeetingAgendas.FindAsync(agendaId);
        if (agenda is null || agenda.Active != "Y") return false;
        // Only log when the action point actually involves the viewer's department.
        if (!await DeptOwnsMeetingAsync(agenda.MeetingRid, deptId)) return false;

        var now = DateTime.Now;
        var view = await _db.TbActionPointViews
            .FirstOrDefaultAsync(v => v.AgendaRid == agendaId && v.DeptId == deptId);
        if (view is null)
        {
            _db.TbActionPointViews.Add(new TbActionPointView
            {
                AgendaRid = agendaId,
                DeptId = deptId,
                FirstViewedAt = now,
                LastViewedAt = now,
                ViewedBy = actor
            });
        }
        else
        {
            view.LastViewedAt = now;
            view.ViewedBy = actor;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    // Overdue, due-today and upcoming (next 3 days) action points for a scoped login (department or
    // officer) — drives the login notification. DueDays = today - dueDate, so >= -3 covers the next 3 days too.
    public async Task<List<ActionablePointDto>> GetMyDuePointsAsync(ScopeRequest? scope = null)
    {
        var all = await GetAllActionablePointsAsync(null, scope);
        return all
            .Where(p => p.AgendaDueDt is not null
                        && !string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                        && p.DueDays >= -3)
            .OrderByDescending(p => p.DueDays)
            .ToList();
    }

    // ---------- All actionable points (cross-meeting) ----------
    public async Task<List<ActionablePointDto>> GetAllActionablePointsAsync(string? statusFilter = null, ScopeRequest? scope = null)
    {
        var scopeRids = await ScopeResolver.ResolveRidsAsync(_db, scope);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Coarse pre-filter: for a scoped login keep only points on meetings that include a scope officer.
        var agendaQuery = _db.TbMeetingAgendas.Where(a => a.Active == "Y");
        if (scopeRids is not null)
            agendaQuery = agendaQuery.Where(a =>
                _db.TbMeetingMembers.Any(mm => mm.MeetingRid == a.MeetingRid && scopeRids.Contains(mm.MemberRid)));

        var rows = await (
            from a in agendaQuery
            join m in _db.TbMeetingSchedules on a.MeetingRid equals m.Rid
            orderby m.MeetingDate descending, a.Rid
            select new
            {
                a.Rid, a.MeetingRid, m.MeetingSubject, m.MeetingDate, m.MeetingPlace,
                a.MeetingAgenda, a.AgendaMembers, a.MemberRids, a.DistrictName, a.AgendaDueDt, a.AgendaStatus,
                a.IsOfficerCalled, a.OfficerRemark,
                RemarksCount = _db.TbRemarksOnAgendas.Count(r => r.AgendaRid == a.Rid)
            }).ToListAsync();

        // Precise per-point filter: only points where a scope officer is actually responsible.
        if (scopeRids is not null)
            rows = rows.Where(a => ParseRids(a.MemberRids).Any(scopeRids.Contains)).ToList();

        var evalMap = await PointEvaluator.EvaluateManyAsync(_db,
            rows.Select(a => new AgendaHeader(a.Rid, a.MemberRids, a.AgendaDueDt, a.AgendaStatus)).ToList(), today);
        var contacts = await OfficerContacts.LoadAsync(_db, rows.Select(a => a.MemberRids));
        var mapped = rows.Select(a =>
        {
            var (status, progress) = evalMap[a.Rid];
            return new ActionablePointDto(
                a.Rid, a.MeetingRid, a.MeetingSubject ?? "", a.MeetingDate, a.MeetingPlace,
                a.MeetingAgenda, a.AgendaMembers, a.DistrictName, a.AgendaDueDt,
                a.AgendaDueDt is null ? 0 : today.DayNumber - a.AgendaDueDt.Value.DayNumber,
                status, a.RemarksCount, progress,
                OfficerContacts.Resolve(a.MemberRids, a.AgendaMembers, contacts),
                a.IsOfficerCalled, a.OfficerRemark);
        });

        if (!string.IsNullOrWhiteSpace(statusFilter))
            mapped = mapped.Where(p => string.Equals(p.Status, statusFilter, StringComparison.OrdinalIgnoreCase));

        return mapped.ToList();
    }

    // Dept logins may only touch action points / ATRs on meetings that include one of their officers.
    // Departments are a flat set per officer (no primary), so this reads the officer-department
    // junction and only falls back to the legacy DeptId column when an officer has no junction rows.
    private Task<bool> DeptOwnsMeetingAsync(int meetingId, int deptId) =>
        _db.TbMeetingMembers
            .Where(mm => mm.MeetingRid == meetingId)
            .Join(_db.TblOfficers, mm => mm.MemberRid, o => o.Rid, (mm, o) => o)
            .AnyAsync(o => o.OfficerDepartments.Any(x => x.Active == "Y")
                ? o.OfficerDepartments.Any(x => x.Active == "Y" && x.DeptId == deptId)
                : o.DeptId == deptId);

    // Action point due date may not fall before the meeting's scheduled date.
    private async Task ValidateDueDateAsync(int meetingId, DateOnly? dueDate)
    {
        if (dueDate is not DateOnly due) return;
        var meetingDate = await _db.TbMeetingSchedules
            .Where(m => m.Rid == meetingId)
            .Select(m => (DateTime?)m.MeetingDate)
            .FirstOrDefaultAsync();
        if (meetingDate is DateTime md && due < DateOnly.FromDateTime(md))
            throw new InvalidOperationException(
                $"Action point due date cannot be earlier than the meeting date ({md:dd MMM yyyy}).");
    }


    // True when the meeting has at least one member officer within the scope rid set.
    private Task<bool> MeetingHasMemberAsync(int meetingId, List<int> scopeRids) =>
        _db.TbMeetingMembers.AnyAsync(mm => mm.MeetingRid == meetingId && scopeRids.Contains(mm.MemberRid));

    private async Task EnsureScopeAccessAsync(int meetingId, List<int>? scopeRids)
    {
        if (scopeRids is not null && !await MeetingHasMemberAsync(meetingId, scopeRids))
            throw new UnauthorizedAccessException(
                "You can only manage action points and ATRs for meetings you are part of.");
    }

    // A completed ATR is always 100%; an in-progress one carries the value the reporter picked,
    // clamped to 0-90 (an in-progress action point can't be "fully done").
    private static int NormalizeProgress(string? status, int? value)
    {
        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            return 100;
        return Math.Clamp(value ?? 0, 0, 90);
    }

    private static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }

    private async Task<(string names, string rids)> BuildMemberCsvAsync(List<int> officerIds)
    {
        var ids = officerIds.Distinct().ToList();
        if (ids.Count == 0) return ("", "");
        var officers = await _db.TblOfficers
            .Where(o => ids.Contains(o.Rid))
            .Select(o => new { o.Rid, o.OfficerName })
            .ToListAsync();
        var ordered = ids.Where(id => officers.Any(o => o.Rid == id)).ToList();
        var names = string.Join(", ", ordered.Select(id => officers.First(o => o.Rid == id).OfficerName));
        var rids = string.Join(",", ordered);
        return (names, rids);
    }
}
