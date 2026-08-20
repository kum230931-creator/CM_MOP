using CMOmeets.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Agendas;

/// One ATR reduced to what completion/progress needs: who reported (MemberRid), its recency (Rid),
/// and the reported progress + status.
public record AtrRow(int? MemberRid, long Rid, int? Progress, string? Status);

/// Just enough of an action point to evaluate it: its id, its responsible-officer CSV, due date, and
/// the stored status flag (used only as a fallback when the point has no ATRs).
public record AgendaHeader(long Rid, string? MemberRids, DateOnly? AgendaDueDt, string AgendaStatus);

/// Computes an action point's status + displayed progress from ALL of its responsible officers, so a
/// point shared by several officers reflects each officer's OWN latest ATR — not merely the most
/// recent ATR from anyone. Rules:
///   * Each assigned officer contributes their own latest ATR's progress (a Completed ATR = 100%,
///     otherwise the reported %); an officer who has never reported contributes 0%.
///   * The point's progress is the AVERAGE across the assigned officers, and the point is Completed
///     ONLY once EVERY assigned officer has completed.
///   * A point with NO ATRs falls back to its stored flag (so legacy backfilled points that were
///     marked Completed with no ATR still read 100%).
///   * A point that has ATRs but none from an assigned officer (or lists no officers) falls back to
///     its single latest ATR — the pre-existing behaviour.
/// This is read live from the ATRs, so points that a single officer prematurely "completed" under the
/// old rule self-correct to their true average with no data migration.
public static class PointEvaluator
{
    public static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }

    private static bool IsCompleted(string? s) => string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase);
    private static int Eff(AtrRow a) => IsCompleted(a.Status) ? 100 : Math.Clamp(a.Progress ?? 0, 0, 100);

    /// True when every assigned officer has a completed latest ATR (used to gate "point is closed").
    public static bool AllOfficersDone(IReadOnlyList<int> officerRids, IReadOnlyList<AtrRow> atrs)
    {
        if (officerRids.Count == 0 || atrs.Count == 0) return false;
        foreach (var off in officerRids)
        {
            var latest = atrs.Where(a => a.MemberRid == off).OrderByDescending(a => a.Rid).FirstOrDefault();
            if (latest is null || !IsCompleted(latest.Status)) return false;
        }
        return true;
    }

    public static (string Status, int Progress) Evaluate(
        IReadOnlyList<int> officerRids, IReadOnlyList<AtrRow> atrs, DateOnly? dueDate, string storedFlag, DateOnly today)
    {
        bool overdue = dueDate is DateOnly d && d < today;
        string OpenStatus() => overdue ? "OverDue" : "InProgress";

        // No ATRs at all -> trust the stored flag (legacy backfilled points marked Completed w/o ATR).
        if (atrs.Count == 0)
        {
            var st = AgendaStatusHelper.Derive(storedFlag, dueDate);
            return (st, IsCompleted(st) ? 100 : 0);
        }

        // Latest ATR per assigned officer (max Rid wins).
        var latestByOfficer = atrs
            .Where(a => a.MemberRid is int m && officerRids.Contains(m))
            .GroupBy(a => a.MemberRid!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Rid).First());

        // Assigned officers, at least one of whom has reported -> average across ALL assigned officers
        // (officers who never reported drag it down with 0%).
        if (officerRids.Count > 0 && latestByOfficer.Count > 0)
        {
            int sum = 0, done = 0;
            foreach (var off in officerRids)
                if (latestByOfficer.TryGetValue(off, out var a)) { sum += Eff(a); if (IsCompleted(a.Status)) done++; }
            bool allDone = done == officerRids.Count;
            int avg = (int)Math.Round((double)sum / officerRids.Count);
            return (allDone ? "Completed" : OpenStatus(), allDone ? 100 : avg);
        }

        // Has ATRs but none from an assigned officer (or no officers listed) -> latest ATR overall.
        var latest = atrs.OrderByDescending(a => a.Rid).First();
        bool ld = IsCompleted(latest.Status);
        return (ld ? "Completed" : OpenStatus(), ld ? 100 : Eff(latest));
    }

    /// Batch-evaluate many points: loads their ATRs (one query) and returns rid -> (status, progress).
    /// For large point sets (whole-DB views) it loads every ATR once instead of emitting a giant
    /// parameter list; for small sets it targets just the points asked for.
    public static async Task<Dictionary<long, (string Status, int Progress)>> EvaluateManyAsync(
        CmoMeetsDbContext db, IReadOnlyCollection<AgendaHeader> headers, DateOnly today)
    {
        var result = new Dictionary<long, (string, int)>(headers.Count);
        if (headers.Count == 0) return result;

        var ids = headers.Select(h => h.Rid).ToHashSet();
        // Keep AgendaRid in the shape so we can group ATRs per point (AtrRow itself drops it).
        var raw = headers.Count > 500
            ? await db.TbRemarksOnAgendas
                .Select(r => new { r.AgendaRid, r.MemberRid, r.Rid, r.ProgressPercentage, r.RemarkStatus })
                .ToListAsync()
            : await db.TbRemarksOnAgendas
                .Where(r => ids.Contains(r.AgendaRid))
                .Select(r => new { r.AgendaRid, r.MemberRid, r.Rid, r.ProgressPercentage, r.RemarkStatus })
                .ToListAsync();

        var byAgenda = raw
            .Where(r => ids.Contains(r.AgendaRid))
            .GroupBy(r => r.AgendaRid)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<AtrRow>)g.Select(x => new AtrRow(x.MemberRid, x.Rid, x.ProgressPercentage, x.RemarkStatus)).ToList());

        IReadOnlyList<AtrRow> empty = Array.Empty<AtrRow>();
        foreach (var h in headers)
            result[h.Rid] = Evaluate(
                ParseRids(h.MemberRids).ToList(),
                byAgenda.TryGetValue(h.Rid, out var list) ? list : empty,
                h.AgendaDueDt, h.AgendaStatus, today);
        return result;
    }
}
