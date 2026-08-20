namespace CMOmeets.Application.Agendas;

public static class AgendaStatusHelper
{
    /// Mirrors the legacy vw_getAllActionablePoints logic: an agenda is Completed only when
    /// explicitly marked; otherwise it is OverDue once its due date has passed, else InProgress.
    public static string Derive(string storedStatus, DateOnly? dueDate)
    {
        if (string.Equals(storedStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            return "Completed";
        if (dueDate is null)
            return "InProgress";
        return dueDate.Value >= DateOnly.FromDateTime(DateTime.Today) ? "InProgress" : "OverDue";
    }

    /// Progress to display for a point: a Completed point always reads 100%; otherwise the
    /// progress reported by its latest ATR (null when none has been recorded yet).
    public static int? DisplayProgress(string derivedStatus, int? rawProgress)
        => string.Equals(derivedStatus, "Completed", StringComparison.OrdinalIgnoreCase) ? 100 : rawProgress;
}
