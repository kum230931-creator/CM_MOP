using CMOmeets.Application.Auth;
using CMOmeets.Domain.Data;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMOmeets.Api.Auth;

public static class IdentitySeeder
{
    // The four active roles + legacy sh/member (seeded but unused). Retired roles `sadmin`/`dept`
    // are migrated to admin/nodal below; their role rows may still exist on older databases.
    private static readonly string[] Roles = { "admin", "cm", "officer", "cmo_officer", "nodal", "sh", "member" };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var db = sp.GetRequiredService<CmoMeetsDbContext>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var legacyUsers = await db.UserAuthenticationDetails
            .Where(u => u.Active == "Y")
            .ToListAsync();

        foreach (var legacy in legacyUsers)
        {
            if (await userManager.FindByNameAsync(legacy.AuthId) is not null)
                continue;

            var user = new AppUser
            {
                UserName = legacy.AuthId,
                Email = $"{legacy.AuthId}@cmomeets.local",
                EmailConfirmed = true,
                DisplayName = legacy.OfficialName,
                Designation = legacy.Designation,
                LocationCode = legacy.LocationCode,
                LocationName = legacy.LocationName,
                IsActive = legacy.Active == "Y",
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded) continue;

            // Preserve the legacy unsalted SHA-512 hash; LegacyPasswordHasher upgrades it on first login.
            user.PasswordHash = LegacyPasswordHasher.LegacyPrefix + legacy.AuthPwd.ToLowerInvariant();
            await userManager.UpdateAsync(user);

            var role = NormalizeRole(legacy.UserType);
            await userManager.AddToRoleAsync(user, role);
        }

        // Migrate any accounts on the retired roles into the new 4-role model.
        await MigrateRoleAsync(userManager, roleManager, "dept", "nodal");
        await MigrateRoleAsync(userManager, roleManager, "sadmin", "admin");

        // Guarantee a usable admin login on a fresh/empty database (no legacy users to migrate).
        if (await userManager.FindByNameAsync("admin") is null)
        {
            var admin = new AppUser
            {
                UserName = "admin",
                Email = "admin@cmomeets.local",
                EmailConfirmed = true,
                DisplayName = "Administrator",
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            if ((await userManager.CreateAsync(admin, "Admin@123")).Succeeded)
                await userManager.AddToRoleAsync(admin, "admin");
        }
    }

    private static string NormalizeRole(string userType)
    {
        var t = userType?.Trim().ToLowerInvariant();
        return t switch
        {
            "sadmin" => "admin",   // super-admin folds into admin under the 4-role model
            "admin" => "admin",
            "sh" => "sh",
            _ => "member"
        };
    }

    // Move every user on a retired role onto its replacement, then drop the old role from them.
    private static async Task MigrateRoleAsync(
        UserManager<AppUser> um, RoleManager<IdentityRole> rm, string fromRole, string toRole)
    {
        if (!await rm.RoleExistsAsync(fromRole)) return;
        foreach (var u in await um.GetUsersInRoleAsync(fromRole))
        {
            if (!await um.IsInRoleAsync(u, toRole)) await um.AddToRoleAsync(u, toRole);
            await um.RemoveFromRoleAsync(u, fromRole);
        }
    }
}
