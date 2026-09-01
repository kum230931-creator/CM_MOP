using CMOmeets.Api.Domain.Entities;
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
    public async Task<List<OfficerDto>> GetOfficersAsync(
        int? deptId = null,
        bool onlyAssigned = false)
    {
        var query = _db.TblOfficers
            .Where(o => o.Active == "Y");

        // Sirf woh officers jinki primary designation assigned hai
        if (onlyAssigned)
        {
            query = query.Where(o => o.DesigId != null);
        }

        // Department filter
        if (deptId != null)
        {
            query = query.Where(o =>
                o.DeptId == deptId ||
                o.OfficerDepartments.Any(x =>
                    x.DeptId == deptId &&
                    x.Active == "Y"));
        }

        // Officers fetch
        var officers = await query
            .AsNoTracking()
            .OrderBy(o => o.Dept.DepartmentName)
            .ThenBy(o => o.Desig != null ? o.Desig.SeqNo : int.MaxValue)
            .ThenBy(o => o.OfficerName)
            .Select(o => new
            {
                o.Rid,
                o.DeptId,
                DepartmentName = o.Dept.DepartmentName,
                o.DesigId,
                DesigName = o.Desig != null
                    ? o.Desig.DesigName
                    : null,
                o.OfficerName,
                o.OfficerMobile,
                o.OfficerEmail,
                Active = o.Active == "Y",

                Departments = o.OfficerDepartments
                    .Where(x => x.Active == "Y")
                    .Select(x => new LookupDto(
                        x.DeptId,
                        x.Dept.DepartmentName))
                    .ToList(),

                Designations = o.OfficerDesignations
                    .Where(x => x.Active == "Y")
                    .OrderBy(x => x.Desig.SeqNo)
                    .Select(x => new LookupDto(
                        x.DesigId,
                        x.Desig.DesigName))
                    .ToList()
            })
            .ToListAsync();

        // Officer IDs
        var officerIds = officers
            .Select(x => x.Rid)
            .ToList();

        // Current ACTIVE mappings only
        var mappings = await _db.TblOfficerMappings
            .AsNoTracking()
            .Where(x =>
                officerIds.Contains(x.OfficerID) &&
                x.Active == "1" &&
                x.DesigID != null)
            .ToListAsync();

        // Department IDs and Designation IDs used in mappings
        var departmentIds = mappings
            .Select(x => x.DeptID)
            .Distinct()
            .ToList();

        var designationIds = mappings
            .Where(x => x.DesigID != null)
            .Select(x => x.DesigID!.Value)
            .Distinct()
            .ToList();

        // Master names
        var departmentNames = await _db.DepartmentMas
            .AsNoTracking()
            .Where(x => departmentIds.Contains(x.Rid))
            .ToDictionaryAsync(
                x => x.Rid,
                x => x.DepartmentName);

        var designationNames = await _db.MasDeptDesignations
            .AsNoTracking()
            .Where(x => designationIds.Contains(x.Rid))
            .ToDictionaryAsync(
                x => x.Rid,
                x => x.DesigName);

        // Final DTO
        return officers
            .Select(o => new OfficerDto(
                o.Rid,
                o.DeptId,
                o.DepartmentName,
                o.DesigId,
                o.DesigName,
                o.OfficerName,
                o.OfficerMobile,
                o.OfficerEmail,
                o.Active,
                o.Departments,
                o.Designations,

                // Exact Dept + Designation mapping
                mappings
                    .Where(m =>
                        m.OfficerID == o.Rid &&
                        m.DesigID != null)
                    .Select(m => new OfficerDepartmentDesignationDto(
                        m.DeptID,
                        departmentNames.TryGetValue(
                            m.DeptID,
                            out var deptName)
                            ? deptName
                            : null,

                        m.DesigID!.Value,
                        designationNames.TryGetValue(
                            m.DesigID.Value,
                            out var desigName)
                            ? desigName
                            : null
                    ))
                    .ToList()
            ))
            .ToList();
    }

    // ---------------------------------------------------------------------
    // Officer <-> tb_meetingAgendas / tb_meetingMembers reconciliation
    //
    // Rule (as decided):
    //  - Non-completed agenda + officer loses a (dept, desig) post (either displaced by
    //    a conflicting new/edited officer, or the officer's own designation is removed
    //    on edit) -> that officer's slot in tb_meetingAgendas.DepartmentIDs is BLANKED
    //    (officerRid -> 0), the (dept, desig) part of the triple is kept as a "vacant post"
    //    marker so a future officer taking the exact same post can be auto-filled in.
    //  - If the officer already completed their own part of that specific agenda
    //    (checked per (agendaRID, memberRID) in tb_remarksOnAgendas), that agenda's
    //    triple is left untouched — history stays.
    //  - tb_meetingMembers works differently by design: once blanked, it is blanked
    //    PERMANENTLY (MemberRid = 0 AND DesignationId = 0) and never auto-refilled.
    //  - When a new/edited officer takes on a (dept, desig) post, any vacant slot for
    //    that exact post in tb_meetingAgendas is auto-filled with them. Multi-officer
    //    agendas are handled per-triple, so only the matching officer's slot changes;
    //    other officers on the same agenda are untouched.
    // ---------------------------------------------------------------------

    // Designation -> its own department (needed since displaced/removed desig lists don't carry deptId).
    private Task<int?> GetDeptIdForDesigAsync(int desigId) =>
        _db.MasDeptDesignations.Where(x => x.Rid == desigId).Select(x => (int?)x.DeptId).FirstOrDefaultAsync();

    // True if this officer already finished their own part of this specific agenda —
    // their history stays untouched even after they transfer/get displaced from the post.
    // One row per (agendaRID, memberRID) in tb_remarksOnAgendas, so a direct lookup is enough.
    private async Task<bool> IsOfficerAgendaCompleteAsync(long agendaRid, int officerId)
    {
        var remark = await _db.TbRemarksOnAgendas
            .FirstOrDefaultAsync(r => r.AgendaRid == agendaRid && r.MemberRid == officerId);

        if (remark is null) return false;

        return remark.ProgressPercentage.GetValueOrDefault() >= 100
            || string.Equals(remark.RemarkStatus, "Completed", StringComparison.OrdinalIgnoreCase);
    }

    // Blanks oldOfficerId's reference (in DepartmentIDs triples, and permanently in
    // tb_meetingMembers) wherever they held (deptId, desigId), for every non-completed
    // agenda. The (dept, desig) part of the tb_meetingAgendas triple is kept as
    // "0:dept:desig" so FillVacantAgendaPostsAsync can find and refill it later.
    private async Task BlankOfficerFromAgendasAsync(int oldOfficerId, int deptId, int desigId)
    {
        // ---- Part 1: tb_meetingAgendas ----
        // Only touched if a matching triple exists there, and skipped if this officer
        // already completed their own part of that specific agenda.
        var agendas = await _db.TbMeetingAgendas
            .Where(a => a.Active == "Y" && a.DepartmentIDs != null && a.DepartmentIDs != "")
            .ToListAsync();

        var agendaTouched = false;
        foreach (var agenda in agendas)
        {
            var triples = ParseChargeTriples(agenda.DepartmentIDs);
            if (!triples.Any(t => t.OfficerRid == oldOfficerId && t.DeptId == deptId && t.DesigId == desigId))
                continue;

            // This officer already completed their own part of THIS agenda — leave it as history.
            if (await IsOfficerAgendaCompleteAsync(agenda.Rid, oldOfficerId))
                continue;

            var updated = triples
                .Select(t => (t.OfficerRid == oldOfficerId && t.DeptId == deptId && t.DesigId == desigId)
                    ? (OfficerRid: 0, t.DeptId, t.DesigId)   // only this officer's triple blanks; others untouched
                    : t)
                .ToList();
            agenda.DepartmentIDs = BuildChargeCsv(updated);

            var rids = updated.Select(t => t.OfficerRid).Where(r => r != 0).Distinct().ToList();
            agenda.MemberRids = string.Join(",", rids);

            var names = await _db.TblOfficers
                .Where(o => rids.Contains(o.Rid))
                .Select(o => new { o.Rid, o.OfficerName })
                .ToListAsync();
            agenda.AgendaMembers = string.Join(", ",
                rids.Where(r => names.Any(n => n.Rid == r))
                    .Select(r => names.First(n => n.Rid == r).OfficerName));

            agendaTouched = true;
        }
        if (agendaTouched)
            await _db.SaveChangesAsync();

        // ---- Part 2: tb_meetingMembers ----
        // Fully independent of tb_meetingAgendas — matches original ClearOfficerRemovedDesignationAsync:
        // blanks by (MemberRid, DesignationId) alone, whether or not any agenda entry exists for this
        // officer, and with no completion check (tb_meetingMembers carries no agenda/remarks reference
        // to check completion against). PERMANENT blank — never auto-refilled.
        var memberRows = await _db.TbMeetingMembers
            .Where(mm => mm.MemberRid == oldOfficerId && mm.DesignationId == desigId)
            .ToListAsync();
        foreach (var row in memberRows)
        {
            row.MemberRid = 0;
            row.DesignationId = 0;   // DepartmentId ko change nahi karna — permanent blank, dobara refill nahi hoga
        }
        if (memberRows.Count > 0)
            await _db.SaveChangesAsync();
    }

    // Finds any vacant (officerRid == 0) slot matching this exact (dept, desig) post across
    // non-completed agendas, and fills it with the officer who now holds that post.
    // Only tb_meetingAgendas is touched here — tb_meetingMembers is intentionally NOT
    // auto-refilled (see BlankOfficerFromAgendasAsync notes above).
    private async Task FillVacantAgendaPostsAsync(int deptId, int desigId, int newOfficerId)
    {
        var agendas = await _db.TbMeetingAgendas
            .Where(a => a.Active == "Y" && a.DepartmentIDs != null && a.DepartmentIDs != "")
            .ToListAsync();

        var touched = new List<TbMeetingAgenda>();
        foreach (var agenda in agendas)
        {
            var triples = ParseChargeTriples(agenda.DepartmentIDs);
            if (!triples.Any(t => t.OfficerRid == 0 && t.DeptId == deptId && t.DesigId == desigId))
                continue;

            var updated = triples
                .Select(t => (t.OfficerRid == 0 && t.DeptId == deptId && t.DesigId == desigId)
                    ? (OfficerRid: newOfficerId, t.DeptId, t.DesigId)   // only this vacant triple fills; others untouched
                    : t)
                .ToList();
            agenda.DepartmentIDs = BuildChargeCsv(updated);

            var rids = updated.Select(t => t.OfficerRid).Where(r => r != 0).Distinct().ToList();
            agenda.MemberRids = string.Join(",", rids);

            var names = await _db.TblOfficers
                .Where(o => rids.Contains(o.Rid))
                .Select(o => new { o.Rid, o.OfficerName })
                .ToListAsync();
            agenda.AgendaMembers = string.Join(", ",
                rids.Where(r => names.Any(n => n.Rid == r))
                    .Select(r => names.First(n => n.Rid == r).OfficerName));

            touched.Add(agenda);
        }
        if (touched.Count == 0) return;

        await _db.SaveChangesAsync();
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

    //create officer Table mapping while create and Edit the officer's info
    private async Task SyncOfficerMappingAsync(int officerId, List<int> depts, List<int> desigs, string actor)
    {
        // Purani active mappings ko deactivate karo (history preserve karte hue)
        var activeMappings = await _db.TblOfficerMappings
            .Where(m => m.OfficerID == officerId && m.Active == "1")
            .ToListAsync();

        foreach (var old in activeMappings)
        {
            old.Active = "0";
            old.EffectiveTo = DateTime.Now;
            old.UpdatedAt = DateTime.Now;
            old.UpdatedBy = actor;
        }

        // Har dept ke liye uske valid designations nikaalo (DeptDesignation-scoped)
        var deptDesigPairs = await _db.MasDeptDesignations
            .Where(x => desigs.Contains(x.Rid) && depts.Contains(x.DeptId))
            .Select(x => new { x.DeptId, DesigId = x.Rid })
            .ToListAsync();

        var primaryDeptId = depts[0];

        foreach (var pair in deptDesigPairs)
        {
            _db.TblOfficerMappings.Add(new TblOfficerMapping
            {
                OfficerID = officerId,
                DeptID = pair.DeptId,
                DesigID = pair.DesigId,
                Active = "1",
                IsPrimary = pair.DeptId == primaryDeptId ? "1" : "0",
                EffectiveFrom = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = actor
            });
        }
        var deptsWithDesig = deptDesigPairs.Select(p => p.DeptId).Distinct().ToList();
        foreach (var deptOnly in depts.Except(deptsWithDesig))
        {
            _db.TblOfficerMappings.Add(new TblOfficerMapping
            {
                OfficerID = officerId,
                DeptID = deptOnly,
                DesigID = null,
                Active = "1",
                IsPrimary = deptOnly == primaryDeptId ? "1" : "0",
                EffectiveFrom = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = actor
            });
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
        await DeactivateDisplacedMappingsAsync(displaced, actor);
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
        await SyncOfficerMappingAsync(entity.Rid, depts, desigs, actor);

        // Anyone displaced from a post by this new officer: blank their non-completed
        // agenda slots for that exact post (history stays for completed ones).
        foreach (var (oldOfficerId, desigId) in displaced)
            if (await GetDeptIdForDesigAsync(desigId) is int dId)
                await BlankOfficerFromAgendasAsync(oldOfficerId, dId, desigId);

        // Fill any vacant agenda slot for every post this new officer now holds
        // (whether just vacated above, or already vacant from an earlier transfer).
        foreach (var desigId in desigs)
            if (await GetDeptIdForDesigAsync(desigId) is int dId)
                await FillVacantAgendaPostsAsync(dId, desigId, entity.Rid);

        return (await GetOfficersAsync()).First(o => o.Rid == entity.Rid);
    }

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

        // FIX: was hardcoded 0, which caused an officer to falsely "conflict" with
        // their own currently-held designation. Must exclude this officer's own id.
        var displaced = await ResolvePostConflictsAsync(desigs, excludeOfficerId: id, dto.Force);
        await DeactivateDisplacedMappingsAsync(displaced, actor);
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
        await SyncOfficerMappingAsync(entity.Rid, depts, desigs, actor);

        // Anyone displaced from a post by this edited officer: blank their non-completed
        // agenda slots for that exact post.
        foreach (var (oldOfficerId, desigId) in displaced)
            if (await GetDeptIdForDesigAsync(desigId) is int dId)
                await BlankOfficerFromAgendasAsync(oldOfficerId, dId, desigId);

        // This officer's own designations that were removed on this edit: blank
        // this officer's non-completed agenda slots for those posts.
        foreach (var desigId in removedDesigIds)
            if (await GetDeptIdForDesigAsync(desigId) is int dId)
                await BlankOfficerFromAgendasAsync(entity.Rid, dId, desigId);

        // Fill any vacant agenda slot for every post this officer now holds after the edit.
        foreach (var desigId in desigs)
            if (await GetDeptIdForDesigAsync(desigId) is int dId)
                await FillVacantAgendaPostsAsync(dId, desigId, entity.Rid);

        return true;
    }

    // An officer must serve at least one department; the first is treated as the primary.
    private static List<int> NormalizeDepartments(IEnumerable<int>? deptIds)
    {
        var list = (deptIds ?? Enumerable.Empty<int>()).Where(d => d > 0).Distinct().ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Select at least one department.");
        return list;
    }

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
    private async Task DeactivateDisplacedMappingsAsync(List<(int OfficerId, int DesigId)> displaced, string actor)
    {
        if (displaced.Count == 0) return;

        var desigIds = displaced.Select(d => d.DesigId).Distinct().ToList();
        var officerIds = displaced.Select(d => d.OfficerId).Distinct().ToList();

        var rows = await _db.TblOfficerMappings
            .Where(m => m.Active == "1"
                     && officerIds.Contains(m.OfficerID)
                     && m.DesigID.HasValue
                     && desigIds.Contains(m.DesigID.Value))
            .ToListAsync();

        // sirf wahi (OfficerId, DesigId) pairs deactivate karo jo displaced list mein hain
        var toDeactivate = rows.Where(r =>
            displaced.Any(d => d.OfficerId == r.OfficerID && d.DesigId == r.DesigID)).ToList();

        foreach (var m in toDeactivate)
        {
            m.Active = "0";
            m.EffectiveTo = DateTime.Now;
            m.UpdatedAt = DateTime.Now;
            m.UpdatedBy = actor;
        }

        await _db.SaveChangesAsync();
    }

    private async Task<List<(int OfficerId, int DesigId)>> ResolvePostConflictsAsync(List<int> desigIds, int excludeOfficerId, bool force)
    {
        var conflicts = await DetectDesignationConflictsAsync(desigIds, excludeOfficerId);
        if (conflicts.Count == 0) return new List<(int, int)>();
        if (!force) throw new DesignationConflictException(conflicts);
        return await ReleaseDesignationsAsync(desigIds, excludeOfficerId);
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
