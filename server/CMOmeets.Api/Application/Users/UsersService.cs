using CMOmeets.Application.Common;
using CMOmeets.Domain.Data;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Application.Users;

public class UsersService
{
    private static readonly string[] AssignableRoles = { "admin", "cm", "officer", "cmo_officer", "nodal" };

    private readonly UserManager<AppUser> _userManager;
    private readonly CmoMeetsDbContext _db;

    public UsersService(UserManager<AppUser> userManager, CmoMeetsDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<List<UserListDto>> GetUsersAsync()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.UserName, u.DisplayName, u.IsActive, u.DepartmentId, u.OfficerId, u.DepartmentIds })
            .ToListAsync();

        var roleMap = await (from ur in _db.UserRoles
                             join r in _db.Roles on ur.RoleId equals r.Id
                             select new { ur.UserId, RoleName = r.Name }).ToListAsync();

        var deptMap = await _db.DepartmentMas
            .Select(d => new { d.Rid, d.DepartmentName })
            .ToListAsync();

        var officerMap = await _db.TblOfficers
            .Select(o => new { o.Rid, o.OfficerName })
            .ToListAsync();

        return users
            .OrderBy(u => u.UserName)
            .Select(u =>
            {
                var deptIds = ScopeResolver.ParseCsvInts(u.DepartmentIds);
                var deptNames = deptIds.Count > 0
                    ? string.Join(", ", deptIds
                        .Select(id => deptMap.FirstOrDefault(d => d.Rid == id)?.DepartmentName)
                        .Where(n => n != null))
                    : null;
                return new UserListDto(
                    u.Id,
                    u.UserName ?? string.Empty,
                    u.DisplayName,
                    roleMap.FirstOrDefault(r => r.UserId == u.Id)?.RoleName ?? string.Empty,
                    u.DepartmentId,
                    u.DepartmentId is int id ? deptMap.FirstOrDefault(d => d.Rid == id)?.DepartmentName : null,
                    u.OfficerId,
                    u.OfficerId is int oid ? officerMap.FirstOrDefault(o => o.Rid == oid)?.OfficerName : null,
                    u.IsActive,
                    deptIds.Count > 0 ? deptIds : null,
                    deptNames);
            })
            .ToList();
    }

    public async Task<UserListDto> CreateAsync(CreateUserDto dto)
    {
        var role = (dto.Role ?? string.Empty).Trim().ToLowerInvariant();
        if (!AssignableRoles.Contains(role))
            throw new InvalidOperationException("Role must be one of admin, cm, officer, cmo_officer or nodal.");
        if (role == "nodal" && dto.DepartmentId is null)
            throw new InvalidOperationException("A department must be selected for a nodal-officer login.");
        if (role == "officer" && dto.OfficerId is null)
            throw new InvalidOperationException("An officer must be selected for an officer login.");
        if (role == "cmo_officer" && (dto.DepartmentIds is null || dto.DepartmentIds.Count == 0))
            throw new InvalidOperationException("At least one department must be selected for a CMO officer login.");
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new InvalidOperationException("Username is required.");
        if (await _userManager.FindByNameAsync(dto.Username.Trim()) is not null)
            throw new InvalidOperationException("That username is already taken.");

        // A scoped account is either department-scoped (nodal) or officer-scoped (officer), never both.
        int? deptId = role == "nodal" ? dto.DepartmentId : null;
        if (deptId is int d && !await _db.DepartmentMas.AnyAsync(x => x.Rid == d))
            throw new InvalidOperationException("Selected department does not exist.");
        int? officerId = role == "officer" ? dto.OfficerId : null;
        if (officerId is int o && !await _db.TblOfficers.AnyAsync(x => x.Rid == o))
            throw new InvalidOperationException("Selected officer does not exist.");

        // A CMO officer stores its directly-chosen department set as a CSV (no department/officer FK).
        string? departmentIdsCsv = null;
        var cmoDeptIds = new List<int>();
        if (role == "cmo_officer")
        {
            cmoDeptIds = dto.DepartmentIds!.Distinct().ToList();
            var existing = await _db.DepartmentMas.Where(x => cmoDeptIds.Contains(x.Rid)).Select(x => x.Rid).ToListAsync();
            if (existing.Count != cmoDeptIds.Count)
                throw new InvalidOperationException("One or more selected departments do not exist.");
            departmentIdsCsv = string.Join(",", cmoDeptIds);
        }

        var user = new AppUser
        {
            UserName = dto.Username.Trim(),
            Email = $"{dto.Username.Trim()}@cmomeets.local",
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username.Trim() : dto.DisplayName.Trim(),
            DepartmentId = deptId,
            OfficerId = officerId,
            DepartmentIds = departmentIdsCsv,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role);

        var deptName = deptId is int id2
            ? await _db.DepartmentMas.Where(x => x.Rid == id2).Select(x => x.DepartmentName).FirstOrDefaultAsync()
            : null;
        var officerName = officerId is int oid2
            ? await _db.TblOfficers.Where(x => x.Rid == oid2).Select(x => x.OfficerName).FirstOrDefaultAsync()
            : null;
        var deptNames = cmoDeptIds.Count > 0
            ? string.Join(", ", await _db.DepartmentMas.Where(x => cmoDeptIds.Contains(x.Rid))
                .OrderBy(x => x.DepartmentName).Select(x => x.DepartmentName).ToListAsync())
            : null;
        return new UserListDto(user.Id, user.UserName!, user.DisplayName, role, deptId, deptName, officerId, officerName, user.IsActive,
            cmoDeptIds.Count > 0 ? cmoDeptIds : null, deptNames);
    }

    public async Task<bool> UpdateAsync(string id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return false;

        var roles = await _userManager.GetRolesAsync(user);
        var isNodal = roles.Contains("nodal");
        var isOfficer = roles.Contains("officer");
        var isCmoOfficer = roles.Contains("cmo_officer");
        if (isNodal && dto.DepartmentId is null)
            throw new InvalidOperationException("A nodal-officer login must keep a department assigned.");
        if (isOfficer && dto.OfficerId is null)
            throw new InvalidOperationException("An officer login must keep an officer assigned.");
        if (isCmoOfficer && (dto.DepartmentIds is null || dto.DepartmentIds.Count == 0))
            throw new InvalidOperationException("A CMO officer login must keep at least one department assigned.");

        user.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? user.UserName! : dto.DisplayName.Trim();
        user.IsActive = dto.IsActive;
        if (isNodal) user.DepartmentId = dto.DepartmentId;
        if (isOfficer) user.OfficerId = dto.OfficerId;
        if (isCmoOfficer)
        {
            var cmoDeptIds = dto.DepartmentIds!.Distinct().ToList();
            var existing = await _db.DepartmentMas.Where(x => cmoDeptIds.Contains(x.Rid)).Select(x => x.Rid).ToListAsync();
            if (existing.Count != cmoDeptIds.Count)
                throw new InvalidOperationException("One or more selected departments do not exist.");
            user.DepartmentIds = string.Join(",", cmoDeptIds);
        }

        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
            throw new InvalidOperationException(string.Join(" ", res.Errors.Select(e => e.Description)));
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return false;

        await _userManager.RemovePasswordAsync(user);
        var res = await _userManager.AddPasswordAsync(user, newPassword);
        if (!res.Succeeded)
            throw new InvalidOperationException(string.Join(" ", res.Errors.Select(e => e.Description)));
        return true;
    }
}
