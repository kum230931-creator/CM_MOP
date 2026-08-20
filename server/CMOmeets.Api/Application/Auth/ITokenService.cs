using CMOmeets.Domain.Identity;

namespace CMOmeets.Application.Auth;

public interface ITokenService
{
    (string token, DateTime expiresAt) CreateToken(AppUser user, IList<string> roles);
}
