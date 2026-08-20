using CMOmeets.Application.Auth;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace CMOmeets.Tests;

public class LegacyPasswordHasherTests
{
    private readonly LegacyPasswordHasher _hasher = new();
    private readonly AppUser _user = new() { UserName = "test" };

    [Fact]
    public void LegacyHash_CorrectPassword_RequiresRehash()
    {
        var stored = LegacyPasswordHasher.LegacyPrefix + LegacyPasswordHasher.Sha512Hex("Secret@123");
        var result = _hasher.VerifyHashedPassword(_user, stored, "Secret@123");
        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Fact]
    public void LegacyHash_IsCaseInsensitive()
    {
        var upper = LegacyPasswordHasher.Sha512Hex("Secret@123").ToUpperInvariant();
        var stored = LegacyPasswordHasher.LegacyPrefix + upper;
        var result = _hasher.VerifyHashedPassword(_user, stored, "Secret@123");
        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Fact]
    public void LegacyHash_WrongPassword_Fails()
    {
        var stored = LegacyPasswordHasher.LegacyPrefix + LegacyPasswordHasher.Sha512Hex("Secret@123");
        var result = _hasher.VerifyHashedPassword(_user, stored, "wrong");
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void ModernHash_RoundTrips()
    {
        var hash = _hasher.HashPassword(_user, "Secret@123");
        Assert.False(hash.StartsWith(LegacyPasswordHasher.LegacyPrefix));
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(_user, hash, "Secret@123"));
    }
}
