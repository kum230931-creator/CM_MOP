using CMOmeets.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Common;

// One concerned officer of an action point: the name plus the mobile number the UI shows after it
// and turns into a tel: link.
public record OfficerContactDto(int Rid, string Name, string? Mobile);

// An action point stores its officers as two snapshots — a rid CSV (memberRids) and a name CSV
// (agendaMembers). Numbers are deliberately NOT snapshotted: they are read from the officer master
// on every request so a number changed in Masters is immediately correct everywhere it is shown.
public static class OfficerContacts
{
    public static IEnumerable<int> ParseRids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var n)) yield return n;
    }

    // One officer lookup for a whole page of action points, keyed by officer rid.
    public static async Task<Dictionary<int, OfficerContactDto>> LoadAsync(
        CmoMeetsDbContext db, IEnumerable<string?> memberRidCsvs)
    {
        var ids = memberRidCsvs.SelectMany(ParseRids).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, OfficerContactDto>();
        var officers = await db.TblOfficers
            .Where(o => ids.Contains(o.Rid))
            .Select(o => new OfficerContactDto(o.Rid, o.OfficerName, o.OfficerMobile))
            .ToListAsync();
        return officers.ToDictionary(o => o.Rid);
    }

    // The officers of one action point, in the order they were assigned. An officer that has since
    // been removed from the master keeps the name recorded on the point, just without a number.
    public static List<OfficerContactDto> Resolve(
        string? memberRidCsv, string? memberNameCsv, IReadOnlyDictionary<int, OfficerContactDto> lookup)
    {
        var rids = ParseRids(memberRidCsv).ToList();
        if (rids.Count == 0) return new List<OfficerContactDto>();
        var names = (memberNameCsv ?? "").Split(',', StringSplitOptions.TrimEntries);
        return rids
            .Select((rid, i) => lookup.TryGetValue(rid, out var officer)
                ? officer
                : new OfficerContactDto(rid, i < names.Length ? names[i] : "", null))
            .ToList();
    }
}
