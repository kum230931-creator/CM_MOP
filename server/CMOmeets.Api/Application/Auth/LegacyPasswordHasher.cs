using System.Security.Cryptography;
using System.Text;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CMOmeets.Application.Auth;

/// Validates accounts migrated from the legacy Web Forms app, where the stored
/// password was an unsalted SHA-512 hex digest of the plaintext. Legacy hashes are
/// seeded with the <see cref="LegacyPrefix"/> marker. On a successful legacy login the
/// caller is told to rehash, transparently upgrading the account to the modern format.
public class LegacyPasswordHasher : IPasswordHasher<AppUser>
{
    public const string LegacyPrefix = "LEG$";
    private readonly PasswordHasher<AppUser> _modern = new();

    public string HashPassword(AppUser user, string password)
        => _modern.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(AppUser user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword.StartsWith(LegacyPrefix, StringComparison.Ordinal))
        {
            var legacyHash = hashedPassword[LegacyPrefix.Length..];
            var computed = Sha512Hex(providedPassword);
            return string.Equals(computed, legacyHash, StringComparison.OrdinalIgnoreCase)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }

        return _modern.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }

    public static string Sha512Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA512.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
