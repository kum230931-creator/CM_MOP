using CMOmeets.Application.Agendas;
using CMOmeets.Application.Auth;
using CMOmeets.Domain.Data;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly CmoMeetsDbContext _db;

    public AuthController(UserManager<AppUser> userManager, ITokenService tokenService, CmoMeetsDbContext db)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid username or password." });

        var verify = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid username or password." });

        // Upgrade legacy SHA-512 hash to the modern Identity format on first successful login.
        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
            await _userManager.UpdateAsync(user);
        }

        return Ok(await BuildResult(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(await ToDtoAsync(user, roles.FirstOrDefault() ?? string.Empty));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return NoContent();
    }

    private async Task<AuthResult> BuildResult(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.CreateToken(user, roles);
        return new AuthResult(token, expiresAt, await ToDtoAsync(user, roles.FirstOrDefault() ?? string.Empty));
    }

    // The departments an officer login serves (its officer's full department set), for the
    // title-bar department switcher. Empty for non-officer logins.
    [Authorize(Roles = "officer,cmo_officer")]
    [HttpGet("my-departments")]
    public async Task<ActionResult<List<OfficerDeptDto>>> MyDepartments()
    {
        var user = await _userManager.GetUserAsync(User);

        // A 'cmo_officer' login has its department set stored directly (CSV), not via an officer record.
        if (User.IsInRole("cmo_officer"))
        {
            var ids = CMOmeets.Application.Common.ScopeResolver.ParseCsvInts(user?.DepartmentIds);
            if (ids.Count == 0) return Ok(new List<OfficerDeptDto>());
            var cmoDepts = await _db.DepartmentMas
                .Where(d => ids.Contains(d.Rid))
                .OrderBy(d => d.DepartmentName)
                .Select(d => new OfficerDeptDto(d.Rid, d.DepartmentName))
                .ToListAsync();
            return Ok(cmoDepts);
        }

        if (user?.OfficerId is not int oid) return Ok(new List<OfficerDeptDto>());

        // (officerID, deptID) is unique in the junction, so no Distinct is needed.
        var depts = await _db.TblOfficerDepartments
            .Where(x => x.OfficerId == oid && x.Active == "Y")
            .Join(_db.DepartmentMas, x => x.DeptId, d => d.Rid, (x, d) => new { d.Rid, d.DepartmentName })
            .OrderBy(d => d.DepartmentName)
            .Select(d => new OfficerDeptDto(d.Rid, d.DepartmentName))
            .ToListAsync();

        // Fallback to the officer's home department if the junction hasn't been populated yet.
        if (depts.Count == 0)
        {
            var home = await _db.TblOfficers.Where(o => o.Rid == oid)
                .Join(_db.DepartmentMas, o => o.DeptId, d => d.Rid, (o, d) => new OfficerDeptDto(d.Rid, d.DepartmentName))
                .FirstOrDefaultAsync();
            if (home is not null) depts.Add(home);
        }
        return Ok(depts);
    }

    private async Task<AuthUserDto> ToDtoAsync(AppUser user, string role)
    {
        string? deptName = user.DepartmentId is int id
            ? await _db.DepartmentMas.Where(d => d.Rid == id).Select(d => d.DepartmentName).FirstOrDefaultAsync()
            : null;

        // For an officer login, surface the officer's name (used in the header / scope banner).
        string? officerName = user.OfficerId is int oid
            ? await _db.TblOfficers.Where(o => o.Rid == oid).Select(o => o.OfficerName).FirstOrDefaultAsync()
            : null;

        return new AuthUserDto(user.UserName ?? string.Empty, user.DisplayName, user.Designation, role,
            user.LocationName, user.DepartmentId, deptName, user.OfficerId, officerName);
    }
}
