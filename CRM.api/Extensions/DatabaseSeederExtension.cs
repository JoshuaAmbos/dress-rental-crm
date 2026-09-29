using CRM.domain.Constants;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CRM.api.Extensions;

public static class DatabaseSeederExtensions
{
    public static async Task SeedIdentityAndTenantsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var masterDb = services.GetRequiredService<MasterCrmDbContext>();
        await masterDb.Database.EnsureCreatedAsync();

        var tenantDb = services.GetRequiredService<TenantCrmDbContext>();
        await tenantDb.Database.EnsureCreatedAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in AppRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        async Task EnsureUserAsync(string username, string email, string password, string role, int companyId, string companyName)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                user = new IdentityUser { UserName = username, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    await userManager.AddClaimAsync(user, new Claim("CompanyId", companyId.ToString()));
                    await userManager.AddClaimAsync(user, new Claim("CompanyName", companyName));
                }
            }
            else
            {
                var claims = await userManager.GetClaimsAsync(user);
                foreach (var claim in claims.Where(c => c.Type is "CompanyId" or "CompanyName"))
                {
                    await userManager.RemoveClaimAsync(user, claim);
                }
                await userManager.AddClaimAsync(user, new Claim("CompanyId", companyId.ToString()));
                await userManager.AddClaimAsync(user, new Claim("CompanyName", companyName));
            }
        }

        // Tenant C: Atelier Haute Couture
        await EnsureUserAsync("superadmin", "superadmin@crm.local", "SuperSecret123!", AppRoles.Superadmin, 1, "Atelier Haute Couture");
        await EnsureUserAsync("atelier_admin", "admin@atelier.local", "SuperSecret123!", AppRoles.Admin, 1, "Atelier Haute Couture");
        await EnsureUserAsync("atelier_manager", "manager@atelier.local", "SuperSecret123!", AppRoles.Manager, 1, "Atelier Haute Couture");
        await EnsureUserAsync("atelier_staff", "staff@atelier.local", "SuperSecret123!", AppRoles.Staff, 1, "Atelier Haute Couture");

        // Tenant B: Maison Étoile Bridal
        await EnsureUserAsync("maison_admin", "admin@maisonetoile.local", "SuperSecret123!", AppRoles.Admin, 2, "Maison Étoile Bridal");
        await EnsureUserAsync("maison_manager", "manager@maisonetoile.local", "SuperSecret123!", AppRoles.Manager, 2, "Maison Étoile Bridal");
        await EnsureUserAsync("maison_staff", "staff@maisonetoile.local", "SuperSecret123!", AppRoles.Staff, 2, "Maison Étoile Bridal");

        // Tenant A: Davao Haute Rentals
        await EnsureUserAsync("davao_admin", "admin@davaohaute.local", "SuperSecret123!", AppRoles.Admin, 3, "Davao Haute Rentals");
        await EnsureUserAsync("davao_manager", "manager@davaohaute.local", "SuperSecret123!", AppRoles.Manager, 3, "Davao Haute Rentals");
        await EnsureUserAsync("davao_staff", "staff@davaohaute.local", "SuperSecret123!", AppRoles.Staff, 3, "Davao Haute Rentals");
    }
}