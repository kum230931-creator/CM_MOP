using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace CMOmeets.Domain.Data;

/// <summary>
/// LocalDB automatic instances shut down when idle and are lazily (and, on low-end
/// machines, unreliably) re-started by the first connection — which can fail with
/// "SQL Server process failed to start". This helper explicitly starts the instance
/// via the SqlLocalDB utility, which is idempotent and waits until the instance is
/// ready, turning that racy lazy start into a controlled one. It is a no-op for
/// connection strings that don't target LocalDB.
/// </summary>
public static class LocalDbStarter
{
    public static void EnsureStarted(string? connectionString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var instance = GetLocalDbInstanceName(connectionString);
        if (instance is null) return; // not a LocalDB connection — nothing to do

        var exe = FindSqlLocalDbExe();
        if (exe is null)
        {
            logger.LogWarning("SqlLocalDB.exe not found; relying on lazy auto-start for instance '{Instance}'.", instance);
            return;
        }

        try
        {
            var psi = new ProcessStartInfo(exe, $"start \"{instance}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null)
            {
                logger.LogWarning("Could not launch SqlLocalDB.exe for instance '{Instance}'.", instance);
                return;
            }
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(60_000);

            if (proc.ExitCode == 0)
                logger.LogInformation("LocalDB instance '{Instance}' is started. {Out}", instance, stdout.Trim());
            else
                logger.LogWarning("SqlLocalDB start for '{Instance}' exited {Code}: {Err}", instance, proc.ExitCode, stderr.Trim());
        }
        catch (Exception ex)
        {
            // Non-fatal: the connection-open retry will still try to bring it up.
            logger.LogWarning(ex, "Failed to pre-start LocalDB instance '{Instance}'.", instance);
        }
    }

    // Returns the LocalDB instance name (e.g. "mssqllocaldb") for a "(localdb)\name"
    // data source, or null when the connection doesn't target LocalDB.
    private static string? GetLocalDbInstanceName(string connectionString)
    {
        string dataSource;
        try { dataSource = new SqlConnectionStringBuilder(connectionString).DataSource; }
        catch { return null; }

        const string prefix = "(localdb)\\";
        var idx = dataSource.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var name = dataSource[(idx + prefix.Length)..].Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private static string? FindSqlLocalDbExe()
    {
        // Check the well-known versioned install folders first (most reliable), then
        // fall back to the bare name for the OS to resolve from PATH.
        foreach (var root in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                 })
        {
            if (string.IsNullOrEmpty(root)) continue;
            foreach (var ver in new[] { "170", "160", "150", "140", "130" })
            {
                var path = Path.Combine(root, "Microsoft SQL Server", ver, "Tools", "Binn", "SqlLocalDB.exe");
                if (File.Exists(path)) return path;
            }
        }
        return "SqlLocalDB.exe"; // last resort: rely on PATH
    }
}
