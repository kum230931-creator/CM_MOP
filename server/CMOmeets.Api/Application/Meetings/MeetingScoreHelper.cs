namespace CMOmeets.Application.Meetings;

public static class MeetingScoreHelper
{
    /// Performance score out of 100 for a meeting. A completed point is worth (100 / total),
    /// an in-progress point half of that, and an overdue point subtracts half — so an all-completed
    /// meeting scores 100 and an all-overdue meeting -50. The score is allowed to go negative as a
    /// penalty; clamped to -100..100. Completion is judged by the caller (actual reported progress,
    /// not the stored status flag) so every screen ranks meetings the same way.
    public static double Compute(int total, int completed, int inProgress, int overdue)
    {
        if (total == 0) return 0.0;
        var weight = 100.0 / total;
        var score = completed * weight + inProgress * (weight / 2) - overdue * (weight / 2);
        return Math.Round(Math.Clamp(score, -100, 100), 1);
    }
}
