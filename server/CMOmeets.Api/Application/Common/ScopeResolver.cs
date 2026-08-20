using CMOmeets.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Common;

// One request's data-scope, built from the caller's role + claims + optional drill-down params.
// Replaces the old (int? deptScope, int? officerScope) pair so the officer role can resolve to a
// SET of departments (multi-department officer) rather than a single officer rid.
public record ScopeRequest(
    bool IsAdmin,
    bool IsCm,
    bool IsNodal,
    bool IsOfficer,
    int? NodalDeptId,          // a nodal login's own department
    int? OfficerLoginId,       // an officer login's officer rid (drives its department set)
    int? RequestedDeptId,      // ?deptId — admin drill-down dept OR an officer's title-bar selection
    int? RequestedOfficerId,   // ?officerId — admin Officer-Dashboard drill-down
    string? ScopeLevel = null,             // advanced admin/cm title-bar filter: "department" | "officer" | "ministry"
    IReadOnlyList<int>? ScopeIds = null,   // the multi-selected ids for ScopeLevel
    bool IsCmoOfficer = false,             // 'cmo_officer' login: directly-chosen multi-department, no post
    IReadOnlyList<int>? CmoDeptIds = null) // its department set (departmentMas.RID)
{
    // Fully-unscoped (admin with no drill-down, or cm) — sees everything.
    public static readonly ScopeRequest Unscoped = new(true, false, false, false, null, null, null, null);

    // The single department a dept-keyed filter should pin to: a nodal's own dept, or an admin/cm's
    // ?deptId title-bar pick. Null for officer/admin-all/cm-all — officers narrow via the resolved rid set.
    public int? DeptFilter => IsNodal ? NodalDeptId : ((IsAdmin || IsCm) ? RequestedDeptId : null);
}

public static class ScopeResolver
{
    // The officers serving ANY of these departments. An officer's departments are a flat set with no
    // primary — `tblOfficer.DeptId` is only a legacy single-value echo — so membership is decided by
    // the officer-department junction, falling back to DeptId for officers that have no junction rows
    // yet (most legacy records). Matching on DeptId alone silently hides a multi-department officer
    // from every department except whichever one happens to sit in that column.
    private static Task<List<int>> OfficerRidsForDeptsAsync(CmoMeetsDbContext db, IEnumerable<int> deptIds)
    {
        var ids = deptIds.Distinct().ToList();
        if (ids.Count == 0) return Task.FromResult(new List<int>());
        return db.TblOfficers
            .Where(o => o.OfficerDepartments.Any(x => x.Active == "Y")
                ? o.OfficerDepartments.Any(x => x.Active == "Y" && ids.Contains(x.DeptId))
                : ids.Contains(o.DeptId))
            .Select(o => o.Rid)
            .ToListAsync();
    }

    // Resolve a scope to the officer rids it may see, or null = unscoped (all).
    public static async Task<List<int>?> ResolveRidsAsync(CmoMeetsDbContext db, ScopeRequest? s)
    {
        if (s is null) return null;

        // Officer login: scope = every officer rid across the officer's departments (its full set,
        // or the one department picked in the title bar when that pick is one of its departments).
        if (s.IsOfficer && s.OfficerLoginId is int oid)
        {
            var depts = await db.TblOfficerDepartments
                .Where(x => x.OfficerId == oid && x.Active == "Y")
                .Select(x => x.DeptId).ToListAsync();
            if (depts.Count == 0)
            {
                // Fallback for an officer with no junction rows yet: its home department.
                var home = await db.TblOfficers.Where(o => o.Rid == oid)
                    .Select(o => (int?)o.DeptId).FirstOrDefaultAsync();
                if (home is int h) depts.Add(h);
            }
            if (s.RequestedDeptId is int sel && depts.Contains(sel))
                depts = new List<int> { sel };
            if (depts.Count == 0) return new List<int>();     // no departments → sees nothing
            return await OfficerRidsForDeptsAsync(db, depts);
        }

        // CMO officer login: scope = officers across its directly-chosen departments (or the one
        // department picked in the title bar when that pick is one of them). No officer record — this
        // mirrors the officer path's dept→officers step. Always scoped (never sees everything).
        if (s.IsCmoOfficer)
        {
            var depts = (s.CmoDeptIds ?? new List<int>()).ToList();
            if (depts.Count == 0) return new List<int>();     // no departments → sees nothing
            if (s.RequestedDeptId is int sel && depts.Contains(sel))
                depts = new List<int> { sel };
            return await OfficerRidsForDeptsAsync(db, depts);
        }

        // Nodal login: every officer serving its department.
        if (s.IsNodal && s.NodalDeptId is int did)
            return await OfficerRidsForDeptsAsync(db, new[] { did });

        // Admin drill-down, or a cm's title-bar department filter: a single officer, or one department.
        // (cm only ever supplies a deptId; admin can also drill into a single officer.)
        if (s.IsAdmin || s.IsCm)
        {
            // Advanced title-bar filter (Level + multi-select) — takes precedence, resolves to the UNION
            // of officer rids across the selected departments / officers / ministries.
            if (!string.IsNullOrEmpty(s.ScopeLevel) && s.ScopeIds is { Count: > 0 })
            {
                var ids = s.ScopeIds.Distinct().ToList();
                switch (s.ScopeLevel.ToLowerInvariant())
                {
                    case "officer":
                        return ids;
                    case "department":
                        return await OfficerRidsForDeptsAsync(db, ids);
                    case "ministry":
                        // Compare against a List<int?> so EF emits a clean `ministryId IN (...)` (nulls
                        // excluded) — `ids.Contains(d.MinistryId.Value)` can fail to translate.
                        var nids = ids.Select(i => (int?)i).ToList();
                        var deptIds = await db.DepartmentMas
                            .Where(d => nids.Contains(d.MinistryId))
                            .Select(d => d.Rid).ToListAsync();
                        return await OfficerRidsForDeptsAsync(db, deptIds);
                }
            }
            if (s.RequestedOfficerId is int roid) return new List<int> { roid };
            if (s.RequestedDeptId is int rdid)
                return await OfficerRidsForDeptsAsync(db, new[] { rdid });
        }

        // Admin or cm with no department picked → unscoped (sees everything).
        return null;
    }

    // Parse a CSV of ints (e.g. AppUser.DepartmentIds / the "deptIds" JWT claim) into distinct values.
    public static List<int> ParseCsvInts(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new List<int>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                 .Where(n => n.HasValue).Select(n => n!.Value).Distinct().ToList();
}
