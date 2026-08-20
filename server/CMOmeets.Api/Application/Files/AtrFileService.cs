using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CMOmeets.Application.Files;

// Stores ATR attachments on disk under {FileStorage:RootPath}/atr and serves them back.
// Files are saved under an opaque GUID name; the original name is never used on disk.
public class AtrFileService
{
    private static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };

    public const long MaxBytes = 10 * 1024 * 1024;

    private readonly string _root;

    public AtrFileService(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["FileStorage:RootPath"] ?? "App_Data/uploads";
        var baseDir = Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
        _root = Path.Combine(baseDir, "atr");
        // The storage directory is created lazily in SaveAsync (not here), so a folder/permission problem
        // can only affect actual uploads — it must never break read endpoints that merely inject this
        // service via DI (e.g. the meetings list, which references this service for the minutes document).
    }

    public bool IsAllowed(string fileName) => Allowed.Contains(Path.GetExtension(fileName));

    public async Task<string> SaveAsync(IFormFile file)
    {
        Directory.CreateDirectory(_root);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(_root, storedName);
        await using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        return storedName;
    }

    // Resolves a stored name to a path, rejecting anything that isn't a bare filename (no traversal).
    public bool TryResolve(string name, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name) return false;
        var candidate = Path.Combine(_root, name);
        if (!File.Exists(candidate)) return false;
        fullPath = candidate;
        return true;
    }

    public static string ContentType(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };
}
