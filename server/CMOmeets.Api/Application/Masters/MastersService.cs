using CMOmeets.Domain.Data;
using CMOmeets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Masters;

public class MastersService
{
    private readonly CmoMeetsDbContext _db;
    public MastersService(CmoMeetsDbContext db) => _db = db;

    private static bool ToBool(string? flag) => string.Equals(flag?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
    private static string ToFlag(bool value) => value ? "Y" : "N";

    // ---------- Ministries ----------
    public async Task<List<MinistryDto>> GetMinistriesAsync() =>
        await _db.MinistryMas
            .OrderBy(m => m.MinistryName)
            .Select(m => new MinistryDto(m.Rid, m.MinistryName, m.DepartmentMas.Count))
            .ToListAsync();

    public async Task<MinistryDto> CreateMinistryAsync(MinistrySaveDto dto)
    {
        var entity = new MinistryMa { MinistryName = dto.MinistryName.Trim(), CreatedAt = DateTime.Now };
        _db.MinistryMas.Add(entity);
        await _db.SaveChangesAsync();
        return new MinistryDto(entity.Rid, entity.MinistryName, 0);
    }

    public async Task<bool> UpdateMinistryAsync(int id, MinistrySaveDto dto)
    {
        var entity = await _db.MinistryMas.FindAsync(id);
        if (entity is null) return false;
        entity.MinistryName = dto.MinistryName.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMinistryAsync(int id)
    {
        var entity = await _db.MinistryMas.Include(m => m.DepartmentMas).FirstOrDefaultAsync(m => m.Rid == id);
        if (entity is null) return false;
        if (entity.DepartmentMas.Any())
            throw new InvalidOperationException("Cannot delete a ministry that has departments.");
        _db.MinistryMas.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Departments ----------
    public async Task<List<DepartmentDto>> GetDepartmentsAsync(int? ministryId = null) =>
        await _db.DepartmentMas
            .Where(d => ministryId == null || d.MinistryId == ministryId)
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentDto(d.Rid, d.MinistryId, d.Ministry!.MinistryName, d.DepartmentName, d.DepartmentNameHin, d.Active == "Y"))
            .ToListAsync();

    public async Task<DepartmentDto?> CreateDepartmentAsync(DepartmentSaveDto dto, string actor)
    {
        var entity = new DepartmentMa
        {
            MinistryId = dto.MinistryId,
            DepartmentName = dto.DepartmentName.Trim(),
            DepartmentNameHin = dto.DepartmentNameHin,
            Active = ToFlag(dto.Active),
            CreatedAt = DateTime.Now,
            CreatedBy = actor
        };
        _db.DepartmentMas.Add(entity);
        await _db.SaveChangesAsync();
        return (await GetDepartmentsAsync()).First(d => d.Rid == entity.Rid);
    }

    public async Task<bool> UpdateDepartmentAsync(int id, DepartmentSaveDto dto)
    {
        var entity = await _db.DepartmentMas.FindAsync(id);
        if (entity is null) return false;
        entity.MinistryId = dto.MinistryId;
        entity.DepartmentName = dto.DepartmentName.Trim();
        entity.DepartmentNameHin = dto.DepartmentNameHin;
        entity.Active = ToFlag(dto.Active);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDepartmentAsync(int id)
    {
        var entity = await _db.DepartmentMas.FindAsync(id);
        if (entity is null) return false;
        entity.Active = "N"; // soft delete
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Designations (department-scoped) ----------
    public async Task<List<DesignationDto>> GetDesignationsAsync(int? deptId = null) =>
        await _db.MasDeptDesignations
            .Where(x => deptId == null || x.DeptId == deptId)
            .OrderBy(x => x.Dept.DepartmentName).ThenBy(x => x.SeqNo)
            .Select(x => new DesignationDto(x.Rid, x.DeptId, x.Dept.DepartmentName, x.DesigName, x.SeqNo, x.Active == "Y"))
            .ToListAsync();

    public async Task<DesignationDto?> CreateDesignationAsync(DesignationSaveDto dto, string actor)
    {
        var entity = new MasDeptDesignation
        {
            DeptId = dto.DeptId,
            DesigName = dto.DesigName.Trim(),
            SeqNo = dto.SeqNo,
            Active = ToFlag(dto.Active),
            CreatedAt = DateTime.Now,
            CreatedBy = actor
        };
        _db.MasDeptDesignations.Add(entity);
        await _db.SaveChangesAsync();
        return (await GetDesignationsAsync()).First(d => d.Rid == entity.Rid);
    }

    public async Task<bool> UpdateDesignationAsync(int id, DesignationSaveDto dto)
    {
        var entity = await _db.MasDeptDesignations.FindAsync(id);
        if (entity is null) return false;
        entity.DeptId = dto.DeptId;
        entity.DesigName = dto.DesigName.Trim();
        entity.SeqNo = dto.SeqNo;
        entity.Active = ToFlag(dto.Active);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDesignationAsync(int id)
    {
        var entity = await _db.MasDeptDesignations.FindAsync(id);
        if (entity is null) return false;
        entity.Active = "N";
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Officers ----------
    // An officer is listed under any department it serves (its primary DeptId or designation id).
    public async Task<List<OfficerDto>> GetOfficersAsync(int? deptId = null) =>
    await _db.TblOfficers
       .Where(o =>
        o.Active == "Y" &&
        (
            deptId == null ||
            o.DeptId == deptId ||
            o.OfficerDepartments.Any(x =>
                x.DeptId == deptId && x.Active == "Y")
        ))
        .OrderBy(o => o.Dept.DepartmentName)
        .ThenBy(o => o.Desig != null ? o.Desig.SeqNo : int.MaxValue)
        .Select(o => new OfficerDto(
            o.Rid,
            o.DeptId,
            o.Dept.DepartmentName,
            o.DesigId,
            o.Desig != null ? o.Desig.DesigName : null,   // ✅ string? — null theek hai
            o.OfficerName,
            o.OfficerMobile,
            o.OfficerEmail,
            o.Active == "Y",
            o.OfficerDepartments.Where(x => x.Active == "Y")
                .Select(x => new LookupDto(x.DeptId, x.Dept.DepartmentName)).ToList(),
            o.OfficerDesignations.Where(x => x.Active == "Y")
                .OrderBy(x => x.Desig.SeqNo)
                .Select(x => new LookupDto(x.DesigId, x.Desig.DesigName)).ToList()))
        .ToListAsync();

    private async Task ClearOfficerRemovedDesignationAsync(
    int officerId,
    List<int> removedDesigIds)
    {
        if (removedDesigIds == null || removedDesigIds.Count == 0)
            return;

        var meetingMembers = await _db.TbMeetingMembers
            .Where(mm =>
                mm.MemberRid == officerId &&
                removedDesigIds.Contains(mm.DesignationId))
            .ToListAsync();

        if (meetingMembers.Count == 0)
            return;

        foreach (var member in meetingMembers)
        {
            member.MemberRid = 0;
            member.DesignationId = 0;

            // DepartmentId ko change nahi karna
        }

        await _db.SaveChangesAsync();
    }
    //create officer and also possible insert the values in mapping table 
    public async Task<OfficerDto?> CreateOfficerAsync(OfficerSaveDto dto, string actor)
    {
        var depts = NormalizeDepartments(dto.DepartmentIds);
        var desigs = NormalizeDesignations(dto.DesignationIds);
        var validDeptIds = await _db.MasDeptDesignations
    .Where(x => desigs.Contains(x.Rid))
    .Select(x => x.DeptId)
    .Distinct()
    .ToListAsync();

        if (validDeptIds.Except(depts).Any())
            throw new InvalidOperationException("Selected designation(s) don't belong to the selected department(s).");
        // A post belongs to one officer: if any picked post is already held, either 409 (unconfirmed)
        // or free it from its current holder (confirmed reassign).
        var displaced = await ResolvePostConflictsAsync(desigs, excludeOfficerId: 0, dto.Force);
        var entity = new TblOfficer
        {
            DeptId = depts[0],   // primary department = first selected (no separate "home" dept anymore)
            DesigId = await ResolvePrimaryDesignationAsync(desigs, depts[0]),
            OfficerName = dto.OfficerName.Trim(),
            OfficerMobile = dto.OfficerMobile.Trim(),
            OfficerEmail = dto.OfficerEmail.Trim(),
            Active = ToFlag(dto.Active),
            CreatedAt = DateTime.Now,
            CreatedBy = actor
        };
        _db.TblOfficers.Add(entity);
        await _db.SaveChangesAsync();
        await SyncOfficerDepartmentsAsync(entity.Rid, depts);
        await SyncOfficerDesignationsAsync(entity.Rid, desigs);
        // Done once the new officer has a rid: the displaced holders' action points follow the post.
        await TransferActionPointsAsync(displaced, entity.Rid);
        return (await GetOfficersAsync()).First(o => o.Rid == entity.Rid);
    }
    //previous method
    //public async Task<bool> UpdateOfficerAsync(int id, OfficerSaveDto dto, string actor)
    //{
    //    var entity = await _db.TblOfficers.FindAsync(id);
    //    if (entity is null) return false;
    //    var depts = NormalizeDepartments(dto.DepartmentIds);
    //    var desigs = NormalizeDesignations(dto.DesignationIds);
    //    var displaced = await ResolvePostConflictsAsync(desigs, excludeOfficerId: id, dto.Force);
    //    entity.DeptId = depts[0];   // primary department = first selected
    //    entity.DesigId = await ResolvePrimaryDesignationAsync(desigs, depts[0]);
    //    entity.OfficerName = dto.OfficerName.Trim();
    //    entity.OfficerMobile = dto.OfficerMobile.Trim();
    //    entity.OfficerEmail = dto.OfficerEmail.Trim();
    //    entity.Active = ToFlag(dto.Active);
    //    entity.UpdatedAt = DateTime.Now;
    //    entity.UpdatedBy = actor;
    //    await _db.SaveChangesAsync();
    //    await SyncOfficerDepartmentsAsync(entity.Rid, depts);
    //    await SyncOfficerDesignationsAsync(entity.Rid, desigs);
    //    await TransferActionPointsAsync(displaced, entity.Rid);
    //    return true;
    //}
    public async Task<bool> UpdateOfficerAsync(int id, OfficerSaveDto dto, string actor)
    {
        var entity = await _db.TblOfficers.FindAsync(id);
        if (entity is null) return false;

        var oldDeptId = entity.DeptId;
        var oldDesigId = entity.DesigId;

        var oldDesigIds = await _db.TblOfficerDesignations
            .Where(x => x.OfficerId == id && x.Active == "Y")
            .Select(x => x.DesigId)
            .ToListAsync();

        var depts = NormalizeDepartments(dto.DepartmentIds);
        var desigs = NormalizeDesignations(dto.DesignationIds);

        var displaced = await ResolvePostConflictsAsync(desigs, excludeOfficerId: 0, dto.Force);
        var newDeptId = depts[0];
        var newDesigId = await ResolvePrimaryDesignationAsync(desigs, newDeptId);

        var removedDesigIds = oldDesigIds.Except(desigs).ToList();
        var chargeChanged = removedDesigIds.Count > 0 || oldDeptId != newDeptId || oldDesigId != newDesigId;

        entity.DeptId = newDeptId;
        entity.DesigId = newDesigId;
        entity.OfficerName = dto.OfficerName.Trim();
        entity.OfficerMobile = dto.OfficerMobile.Trim();
        entity.OfficerEmail = dto.OfficerEmail.Trim();
        entity.Active = ToFlag(dto.Active);
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = actor;

        await _db.SaveChangesAsync();

        await SyncOfficerDepartmentsAsync(entity.Rid, depts);
        await SyncOfficerDesignationsAsync(entity.Rid, desigs);

        if (removedDesigIds.Count > 0)
        {
            await ClearOfficerRemovedDesignationAsync(
                entity.Rid,
                removedDesigIds);
        }

        return true;
    }
    private async Task HandleOfficerChargeChangeAsync(int officerId, List<int> removedDesigIds)
    {
        if (removedDesigIds.Count == 0) return;

        var agendas = await _db.TbMeetingAgendas
            .Where(a => a.Active == "Y" && a.DepartmentIDs != null && a.DepartmentIDs != "")
            .ToListAsync();

        // Sirf woh agendas jahan officer ki EXACT removed designation wali triple maujood hai
        var affectedAgendas = agendas
            .Where(a => ParseChargeTriples(a.DepartmentIDs)
                .Any(t => t.OfficerRid == officerId && removedDesigIds.Contains(t.DesigId)))
            .ToList();

        if (affectedAgendas.Count == 0) return;

        var agendaIds = affectedAgendas.Select(a => a.Rid).ToList();

        var remarks = await _db.TbRemarksOnAgendas
            .Where(r => agendaIds.Contains(r.AgendaRid))
            .OrderByDescending(r => r.Rid)
            .ToListAsync();

        var latestRemarks = remarks
            .GroupBy(r => r.AgendaRid)
            .ToDictionary(g => g.Key, g => g.First());

        var pendingAgendas = affectedAgendas
            .Where(a =>
                !latestRemarks.TryGetValue(a.Rid, out var remark) ||
                ((remark.ProgressPercentage ?? 0) < 100 &&
                 !string.Equals(remark.RemarkStatus, "Completed", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (pendingAgendas.Count == 0) return;

        var pendingMeetingIds = pendingAgendas.Select(a => a.MeetingRid).Distinct().ToList();

        // ---------------------------------------------------------
        // 1. TbMeetingMembers — sirf removed designation wali row clear (already precise, DesignationId column se)
        // ---------------------------------------------------------
        var meetingMembers = await _db.TbMeetingMembers
            .Where(mm =>
                mm.MemberRid == officerId &&
                pendingMeetingIds.Contains(mm.MeetingRid) &&
                removedDesigIds.Contains(mm.DesignationId))
            .ToListAsync();

        foreach (var member in meetingMembers)
        {
            member.MemberRid = 0;
            member.DesignationId = 0;
            // DepartmentId same rahega
        }

        // ---------------------------------------------------------
        // 2. TbMeetingAgendas — DepartmentIDs se sirf officer+removedDesig wali triple(s) hatao,
        //    officer ki baaki (abhi bhi valid) designation ki triple UNTOUCHED rahegi
        // ---------------------------------------------------------
        foreach (var agenda in pendingAgendas)
        {
            var triples = ParseChargeTriples(agenda.DepartmentIDs);

            var remainingTriples = triples
                .Where(t => !(t.OfficerRid == officerId && removedDesigIds.Contains(t.DesigId)))
                .ToList();

            if (remainingTriples.Count == triples.Count)
                continue; // safety: is agenda me kuch remove hua hi nahi

            agenda.DepartmentIDs = remainingTriples.Count > 0 ? BuildChargeCsv(remainingTriples) : null;

            // memberRIDs / agendaMembers ko remaining triples ke DISTINCT officer rids se rebuild karo
            var remainingOfficerRids = remainingTriples.Select(t => t.OfficerRid).Distinct().ToList();

            agenda.MemberRids = remainingOfficerRids.Count > 0
                ? string.Join(",", remainingOfficerRids)
                : null;

            if (remainingOfficerRids.Count > 0)
            {
                var remainingNames = await _db.TblOfficers
                    .Where(o => remainingOfficerRids.Contains(o.Rid))
                    .Select(o => o.OfficerName)
                    .ToListAsync();
                agenda.AgendaMembers = string.Join(", ", remainingNames);
            }
            else
            {
                agenda.AgendaMembers = null;
            }
        }

        await _db.SaveChangesAsync();
    }
    // An officer must serve at least one department; the first is treated as the primary.
    private static List<int> NormalizeDepartments(IEnumerable<int>? deptIds)
    {
        var list = (deptIds ?? Enumerable.Empty<int>()).Where(d => d > 0).Distinct().ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Select at least one department.");
        return list;
    }
    // "officerRid:deptId:desigId" triples ko parse karta hai new method fot tbl_agenda
    private static List<(int OfficerRid, int DeptId, int DesigId)> ParseChargeTriples(string? csv)
    {
        var result = new List<(int, int, int)>();
        if (string.IsNullOrWhiteSpace(csv)) return result;

        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split(':', StringSplitOptions.TrimEntries);
            if (pieces.Length == 3 &&
                int.TryParse(pieces[0], out var officerRid) &&
                int.TryParse(pieces[1], out var deptId) &&
                int.TryParse(pieces[2], out var desigId))
            {
                result.Add((officerRid, deptId, desigId));
            }
        }
        return result;
    }

    private static string BuildChargeCsv(IEnumerable<(int OfficerRid, int DeptId, int DesigId)> triples) =>
        string.Join(",", triples.Select(t => $"{t.OfficerRid}:{t.DeptId}:{t.DesigId}"));

    // An officer must hold at least one designation (one per department they serve).
    private static List<int> NormalizeDesignations(IEnumerable<int>? desigIds)
    {
        var list = (desigIds ?? Enumerable.Empty<int>()).Where(d => d > 0).Distinct().ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Select at least one designation.");
        return list;
    }

    // The primary designation (stored on TblOfficer.DesigId) is the selected designation belonging to
    // the primary department, or the first selected one if none matches.
    private async Task<int> ResolvePrimaryDesignationAsync(List<int> desigIds, int primaryDeptId)
    {
        var byDept = await _db.MasDeptDesignations
            .Where(x => desigIds.Contains(x.Rid))
            .Select(x => new { x.Rid, x.DeptId })
            .ToListAsync();
        var match = byDept.FirstOrDefault(x => x.DeptId == primaryDeptId);
        return match?.Rid ?? desigIds[0];
    }

    // Reconcile an officer's department set (junction rows) to exactly the selected departments.
    private async Task SyncOfficerDepartmentsAsync(int officerId, IEnumerable<int> deptIds)
    {
        var desired = deptIds.Distinct().ToHashSet();
        var existing = await _db.TblOfficerDepartments.Where(x => x.OfficerId == officerId).ToListAsync();
        foreach (var e in existing.Where(e => !desired.Contains(e.DeptId)))
            _db.TblOfficerDepartments.Remove(e);
        foreach (var did in desired)
        {
            var match = existing.FirstOrDefault(e => e.DeptId == did);
            if (match is null)
                _db.TblOfficerDepartments.Add(new TblOfficerDepartment { OfficerId = officerId, DeptId = did, Active = "Y" });
            else match.Active = "Y";
        }
        await _db.SaveChangesAsync();
    }

    // Reconcile an officer's designation set (junction rows) to exactly the selected designations.
    private async Task SyncOfficerDesignationsAsync(int officerId, IEnumerable<int> desigIds)
    {
        var desired = desigIds.Distinct().ToHashSet();
        var existing = await _db.TblOfficerDesignations.Where(x => x.OfficerId == officerId).ToListAsync();
        foreach (var e in existing.Where(e => !desired.Contains(e.DesigId)))
            _db.TblOfficerDesignations.Remove(e);
        foreach (var did in desired)
        {
            var match = existing.FirstOrDefault(e => e.DesigId == did);
            if (match is null)
                _db.TblOfficerDesignations.Add(new TblOfficerDesignation { OfficerId = officerId, DesigId = did, Active = "Y" });
            else match.Active = "Y";
        }
        await _db.SaveChangesAsync();
    }

    // Posts (designations) picked here that are already held by ANOTHER active officer. Excludes the
    // officer being edited (excludeOfficerId; 0 when creating).
    private async Task<List<DesignationConflictDto>> DetectDesignationConflictsAsync(List<int> desigIds, int excludeOfficerId)
        => await _db.TblOfficerDesignations
            .Where(x => x.Active == "Y" && desigIds.Contains(x.DesigId) && x.OfficerId != excludeOfficerId && x.Officer.Active == "Y")
            .Select(x => new DesignationConflictDto(x.DesigId, x.Desig.DesigName, x.OfficerId, x.Officer.OfficerName))
            .Distinct()
            .ToListAsync();

    
    private async Task<List<(int OfficerId, int DesigId)>> ReleaseDesignationsAsync(List<int> desigIds, int excludeOfficerId)
    {
        var rows = await _db.TblOfficerDesignations
            .Where(x => x.Active == "Y" && desigIds.Contains(x.DesigId) && x.OfficerId != excludeOfficerId)
            .ToListAsync();
        if (rows.Count == 0) return new List<(int, int)>();

        var displacedPairs = rows.Select(r => (r.OfficerId, r.DesigId)).ToList();

        _db.TblOfficerDesignations.RemoveRange(rows);
        await _db.SaveChangesAsync();

        var affectedOfficerIds = displacedPairs.Select(p => p.OfficerId).Distinct().ToList();
        foreach (var oid in affectedOfficerIds)
        {
            var officer = await _db.TblOfficers.FindAsync(oid);
            if (officer is null || officer.DesigId is not int primary || !desigIds.Contains(primary)) continue;
            var remaining = await _db.TblOfficerDesignations
                .Where(x => x.OfficerId == oid && x.Active == "Y").Select(x => x.DesigId).ToListAsync();
            officer.DesigId = remaining.Count > 0 ? remaining[0] : (int?)null;
        }
        await _db.SaveChangesAsync();
        return displacedPairs;
    }

    private async Task<List<(int OfficerId, int DesigId)>> ResolvePostConflictsAsync(List<int> desigIds, int excludeOfficerId, bool force)
    {
        var conflicts = await DetectDesignationConflictsAsync(desigIds, excludeOfficerId);
        if (conflicts.Count == 0) return new List<(int, int)>();
        if (!force) throw new DesignationConflictException(conflicts);
        return await ReleaseDesignationsAsync(desigIds, excludeOfficerId);
    }

    // A post carries its work: when an officer is transferred out and another takes their post, every
    // action point the former holder was responsible for moves to the new holder, so the pending work
    // shows up under the person who now has to do it rather than under someone who has left.
    //
    // Deliberately NOT rewritten: the ATRs already filed (tb_remarksOnAgendas keeps the reporting
    // officer's rid and name) — those are the historical record of who actually did the work.
    //private async Task TransferActionPointsAsync(IReadOnlyCollection<int> fromOfficerIds, int toOfficerId)
    //{
    //    var from = fromOfficerIds.Where(id => id != toOfficerId).ToHashSet();
    //    if (from.Count == 0) return;

    //    // Points name officers as a CSV of rids, so the match has to be made on parsed values —
    //    // a substring test would let rid 8 match "8174".
    //    var candidates = await _db.TbMeetingAgendas
    //        .Where(a => a.Active == "Y" && a.MemberRids != null && a.MemberRids != "")
    //        .Select(a => new { a.Rid, a.MemberRids })
    //        .ToListAsync();
    //    var affectedIds = candidates
    //        .Where(a => ParseRids(a.MemberRids).Any(from.Contains))
    //        .Select(a => a.Rid)
    //        .ToList();
    //    if (affectedIds.Count == 0) return;

    //    var points = await _db.TbMeetingAgendas.Where(a => affectedIds.Contains(a.Rid)).ToListAsync();

    //    // AgendaMembers is a display snapshot of the names, so it has to be rebuilt alongside the rids.
    //    var newRids = points.SelectMany(p => ParseRids(p.MemberRids))
    //        .Select(r => from.Contains(r) ? toOfficerId : r).Distinct().ToList();
    //    var names = (await _db.TblOfficers.Where(o => newRids.Contains(o.Rid))
    //            .Select(o => new { o.Rid, o.OfficerName }).ToListAsync())
    //        .ToDictionary(o => o.Rid, o => o.OfficerName);

    //    foreach (var p in points)
    //    {
    //        var mapped = new List<int>();
    //        foreach (var rid in ParseRids(p.MemberRids))
    //        {
    //            // The new holder may already be named on the point — keep one entry, not two.
    //            var target = from.Contains(rid) ? toOfficerId : rid;
    //            if (!mapped.Contains(target)) mapped.Add(target);
    //        }
    //        p.MemberRids = string.Join(",", mapped);
    //        p.AgendaMembers = string.Join(", ", mapped.Where(names.ContainsKey).Select(id => names[id]));
    //    }

    //    // The "Responsible Officer" list on a point is drawn from the meeting's members, so the new
    //    // holder has to be one — otherwise the point they just inherited has no valid owner on screen.
    //    // The former holder stays a member: their attendance at that meeting is a matter of record.
    //    var meetingIds = points.Select(p => p.MeetingRid).Distinct().ToList();
    //    var already = await _db.TbMeetingMembers
    //        .Where(mm => meetingIds.Contains(mm.MeetingRid) && mm.MemberRid == toOfficerId)
    //        .Select(mm => mm.MeetingRid).ToListAsync();
    //    foreach (var mid in meetingIds.Except(already))
    //        _db.TbMeetingMembers.Add(new TbMeetingMember { MeetingRid = mid, MemberRid = toOfficerId, AddedAt = DateTime.Now });

    //    await _db.SaveChangesAsync();
    //}
    //new method create 
    private async Task TransferActionPointsAsync(List<(int OfficerId, int DesigId)> displacedPairs, int toOfficerId)
    {
        var pairs = displacedPairs.Where(p => p.OfficerId != toOfficerId).ToList();
        if (pairs.Count == 0) return;

        var agendas = await _db.TbMeetingAgendas
            .Where(a => a.Active == "Y" && a.DepartmentIDs != null && a.DepartmentIDs != "")
            .ToListAsync();

        var affectedAgendas = agendas
            .Where(a => ParseChargeTriples(a.DepartmentIDs)
                .Any(t => pairs.Any(p => p.OfficerId == t.OfficerRid && p.DesigId == t.DesigId)))
            .ToList();

        if (affectedAgendas.Count == 0) return;

        foreach (var agenda in affectedAgendas)
        {
            var triples = ParseChargeTriples(agenda.DepartmentIDs);

            // sirf woh triple(s) update hongi jinka (officer, desig) displaced list me match karta hai
            var updatedTriples = triples
                .Select(t => pairs.Any(p => p.OfficerId == t.OfficerRid && p.DesigId == t.DesigId)
                    ? (OfficerRid: toOfficerId, t.DeptId, t.DesigId)   // sirf rid badla, dept/desig same
                    : t)
                .ToList();

            agenda.DepartmentIDs = BuildChargeCsv(updatedTriples);

            var distinctOfficerRids = updatedTriples.Select(t => t.OfficerRid).Distinct().ToList();
            agenda.MemberRids = string.Join(",", distinctOfficerRids);

            var names = await _db.TblOfficers
                .Where(o => distinctOfficerRids.Contains(o.Rid))
                .Select(o => new { o.Rid, o.OfficerName })
                .ToListAsync();
            agenda.AgendaMembers = string.Join(", ",
                distinctOfficerRids.Where(id => names.Any(n => n.Rid == id))
                                    .Select(id => names.First(n => n.Rid == id).OfficerName));
        }

        // TbMeetingMembers — sirf displaced (officer, desig) wali row ka MemberRid naye officer ko do,
        // baaki rows (uski dusri designations) touch hi nahi hongi
        var pendingMeetingIds = affectedAgendas.Select(a => a.MeetingRid).Distinct().ToList();
        foreach (var (oldOfficerId, desigId) in pairs)
        {
            var rows = await _db.TbMeetingMembers
                .Where(mm => mm.MemberRid == oldOfficerId &&
                             mm.DesignationId == desigId &&
                             pendingMeetingIds.Contains(mm.MeetingRid))
                .ToListAsync();

            foreach (var row in rows)
                row.MemberRid = toOfficerId;   // DesignationId/DepartmentId same rahega
        }

        await _db.SaveChangesAsync();
    }


    private static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }

    public async Task<bool> DeleteOfficerAsync(int id)
    {
        var entity = await _db.TblOfficers.FindAsync(id);
        if (entity is null) return false;
        entity.Active = "N";
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Districts ----------
    public async Task<List<DistrictDto>> GetDistrictsAsync() =>
        await _db.MasterDistricts
            .OrderBy(d => d.DName)
            .Select(d => new DistrictDto(d.DCode, d.DName, d.IsActive == "Y"))
            .ToListAsync();

    public async Task<DistrictDto?> CreateDistrictAsync(DistrictSaveDto dto)
    {
        if (await _db.MasterDistricts.AnyAsync(d => d.DCode == dto.DCode))
            throw new InvalidOperationException("A district with this code already exists.");
        var entity = new MasterDistrict { DCode = dto.DCode.Trim(), DName = dto.DName.Trim(), IsActive = ToFlag(dto.IsActive) };
        _db.MasterDistricts.Add(entity);
        await _db.SaveChangesAsync();
        return new DistrictDto(entity.DCode, entity.DName, entity.IsActive == "Y");
    }

    public async Task<bool> UpdateDistrictAsync(string code, DistrictSaveDto dto)
    {
        var entity = await _db.MasterDistricts.FirstOrDefaultAsync(d => d.DCode == code);
        if (entity is null) return false;
        entity.DName = dto.DName.Trim();
        entity.IsActive = ToFlag(dto.IsActive);
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Lookups ----------
    public async Task<List<LookupDto>> GetMinistryLookupAsync() =>
        await _db.MinistryMas.OrderBy(m => m.MinistryName)
            .Select(m => new LookupDto(m.Rid, m.MinistryName)).ToListAsync();

    public async Task<List<LookupDto>> GetDepartmentLookupAsync() =>
        await _db.DepartmentMas.Where(d => d.Active == "Y").OrderBy(d => d.DepartmentName)
            .Select(d => new LookupDto(d.Rid, d.DepartmentName)).ToListAsync();

    public async Task<List<LookupDto>> GetDesignationLookupAsync(int deptId) =>
        await _db.MasDeptDesignations.Where(x => x.DeptId == deptId && x.Active == "Y").OrderBy(x => x.SeqNo)
            .Select(x => new LookupDto(x.Rid, x.DesigName)).ToListAsync();

    // Every active designation across all departments, labelled with its department to disambiguate
    // same-named designations. Used by the officer dialog where designation is department-independent.
    public async Task<List<LookupDto>> GetAllDesignationLookupAsync() =>
        await _db.MasDeptDesignations.Where(x => x.Active == "Y")
            .OrderBy(x => x.Dept.DepartmentName).ThenBy(x => x.SeqNo)
            .Select(x => new LookupDto(x.Rid, x.DesigName + " (" + x.Dept.DepartmentName + ")"))
            .ToListAsync();
}
