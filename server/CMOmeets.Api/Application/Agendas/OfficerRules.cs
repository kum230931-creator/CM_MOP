namespace CMOmeets.Application.Agendas;

public static class OfficerRules
{
    // An officer whose designation is ACS (Additional Chief Secretary) may create action points.
    // Designation text is admin-configured (MAS_DeptDesignation.desigName); match the common spellings.
    public static bool IsAcs(string? designation)
    {
        if (string.IsNullOrWhiteSpace(designation)) return false;
        var d = designation.Trim();
        return d.Equals("ACS", StringComparison.OrdinalIgnoreCase)
            || d.Equals("Additional Chief Secretary", StringComparison.OrdinalIgnoreCase);
    }
}
