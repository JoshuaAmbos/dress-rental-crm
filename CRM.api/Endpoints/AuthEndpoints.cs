using CRM.api.DTOs;
using CRM.domain.Constants;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CRM.api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // -------------------------------------------------------------
        // 1. AUTHENTICATION (/api/auth)
        // -------------------------------------------------------------
        var authGroup = routes.MapGroup("/api/auth");

        authGroup.MapPost("/login", async (
            LoginRequestDto request,
            UserManager<IdentityUser> userManager,
            MasterCrmDbContext masterDb) =>
        {
            var user = await userManager.FindByNameAsync(request.Username);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            var claims = await userManager.GetClaimsAsync(user);

            var companyIdClaim = claims.FirstOrDefault(c => c.Type == "CompanyId")?.Value;
            var companyNameClaim = claims.FirstOrDefault(c => c.Type == "CompanyName")?.Value;

            int companyId = int.TryParse(companyIdClaim, out var cid) ? cid : 1;
            string companyName = companyNameClaim ?? "Atelier Haute Couture";

            // Resolve company subscription & database routing from Master CRM
            var company = await masterDb.Companies
                .Include(c => c.SubscriptionPackage)
                .Include(c => c.CompanyDatabases)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            // =========================================================================
            // DEACTIVATED TENANT LOCKOUT GATE
            // =========================================================================
            bool isSuperAdmin = roles.Any(r =>
                r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

            // Non-Superadmin users (Admin, Manager, Staff) cannot access deactivated boutiques
            if (!isSuperAdmin)
            {
                if (company == null || !company.IsActive)
                {
                    return Results.Json(
                        new { message = "Access Denied: This boutique tenant is currently deactivated. Please contact platform administration." },
                        statusCode: StatusCodes.Status403Forbidden);
                }
            }

            var pkg = company?.SubscriptionPackage;
            var activeDb = company?.CompanyDatabases.FirstOrDefault(d => d.IsActive);

            return Results.Ok(new LoginResult
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CompanyId = companyId,
                CompanyName = company?.CompanyName ?? companyName,
                Roles = [.. roles],
                PackageName = pkg?.PackageName ?? "Package A - Complete",
                MaxBranches = pkg?.MaxBranches ?? 99,
                HasMultiBranch = pkg?.HasMultiBranch ?? true,
                HasLoyalty = pkg?.HasLoyalty ?? true,
                HasAnalytics = pkg?.HasAnalytics ?? true,
                DatabaseName = activeDb?.DatabaseName ?? $"DB_Tenant_{company?.CompanyCode ?? "CRM"}",
                ServerName = activeDb?.ServerName ?? "localhost,1433"
            });
        });

        // -------------------------------------------------------------
        // 2. USER REGISTRATION
        // -------------------------------------------------------------
        var registerDelegate = async (
            CreateUserRequestDto request,
            UserManager<IdentityUser> userManager) =>
        {
            if (request.Role.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Creation of Superadmin accounts is strictly prohibited." });
            }

            if (!new[] { AppRoles.Staff, AppRoles.Manager, AppRoles.Admin }.Contains(request.Role))
            {
                return Results.BadRequest(new { message = $"Role '{request.Role}' is invalid." });
            }

            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$");
            if (string.IsNullOrWhiteSpace(request.Password) || !passwordRegex.IsMatch(request.Password))
            {
                return Results.BadRequest(new
                {
                    message = "Password must be at least 8 characters long and contain at least 1 uppercase letter, 1 lowercase letter, 1 digit, and 1 special character."
                });
            }

            var existingUser = await userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                return Results.Conflict(new { message = $"Username '{request.Username}' is already taken." });
            }

            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return Results.Conflict(new { message = $"Email '{request.Email}' is already registered." });
            }

            var user = new IdentityUser
            {
                UserName = request.Username.Trim(),
                Email = request.Email.Trim(),
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return Results.BadRequest(new { message = string.Join("; ", createResult.Errors.Select(e => e.Description)) });
            }

            await userManager.AddToRoleAsync(user, request.Role);
            await userManager.AddClaimAsync(user, new Claim("CompanyId", request.CompanyId.ToString()));
            await userManager.AddClaimAsync(user, new Claim("CompanyName", request.CompanyName));

            return Results.Created($"/api/users/{user.Id}", new
            {
                userId = user.Id,
                username = user.UserName,
                email = user.Email,
                role = request.Role,
                companyId = request.CompanyId,
                companyName = request.CompanyName
            });
        };

        routes.MapPost("/api/users/register", registerDelegate);
        routes.MapPost("/api/auth/register", registerDelegate);

        // -------------------------------------------------------------
        // 3. ASSIGN / MOVE USER TO SHOWROOM BRANCH
        // -------------------------------------------------------------
        routes.MapPut("/api/users/{userId}/branch", async (
            string userId,
            AssignBranchRequestDto request,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found." });
            }

            if (user.UserName?.Equals("superadmin", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.BadRequest(new { message = "Superadmin showroom assignment cannot be altered." });
            }

            var claims = await userManager.GetClaimsAsync(user);

            // Strip existing branch claims
            var oldBranchClaims = claims.Where(c => c.Type is "BranchId" or "BranchName").ToList();
            foreach (var c in oldBranchClaims)
            {
                await userManager.RemoveClaimAsync(user, c);
            }

            // Assign new branch claims if specified
            if (request.BranchId.HasValue && !string.IsNullOrWhiteSpace(request.BranchName))
            {
                await userManager.AddClaimAsync(user, new Claim("BranchId", request.BranchId.Value.ToString()));
                await userManager.AddClaimAsync(user, new Claim("BranchName", request.BranchName.Trim()));
            }

            return Results.Ok(new
            {
                message = request.BranchId.HasValue
                    ? $"User moved to showroom '{request.BranchName}'."
                    : "User reassigned to All Showrooms (Unassigned)."
            });
        });

        // -------------------------------------------------------------
        // 4. USER PROFILE CRUD (Edit, Reset Password, Delete)
        // -------------------------------------------------------------
        routes.MapPut("/api/users/{userId}", async (
            string userId,
            UpdateUserRequestDto request,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            if (user.UserName?.Equals("superadmin", StringComparison.OrdinalIgnoreCase) == true)
                return Results.BadRequest(new { message = "Superadmin account cannot be modified." });

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                user.Email = request.Email.Trim();
                await userManager.UpdateAsync(user);
            }

            if (!string.IsNullOrWhiteSpace(request.Role) && new[] { AppRoles.Staff, AppRoles.Manager, AppRoles.Admin }.Contains(request.Role))
            {
                var currentRoles = await userManager.GetRolesAsync(user);
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                await userManager.AddToRoleAsync(user, request.Role);
            }

            var claims = await userManager.GetClaimsAsync(user);
            var oldTenantClaims = claims.Where(c => c.Type is "CompanyId" or "CompanyName").ToList();
            foreach (var c in oldTenantClaims) await userManager.RemoveClaimAsync(user, c);

            await userManager.AddClaimAsync(user, new Claim("CompanyId", request.CompanyId.ToString()));
            await userManager.AddClaimAsync(user, new Claim("CompanyName", request.CompanyName));

            return Results.Ok(new { message = "User updated successfully." });
        });

        routes.MapPost("/api/users/{userId}/reset-password", async (
            string userId,
            ResetPasswordRequestDto request,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$");
            if (!passwordRegex.IsMatch(request.NewPassword))
            {
                return Results.BadRequest(new { message = "Password must be at least 8 chars with 1 uppercase, 1 lowercase, 1 digit, and 1 special char." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            return Results.Ok(new { message = "Password reset successfully." });
        });

        routes.MapDelete("/api/users/{userId}", async (
            string userId,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            if (user.UserName?.Equals("superadmin", StringComparison.OrdinalIgnoreCase) == true)
                return Results.BadRequest(new { message = "Superadmin account cannot be deleted." });

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            return Results.Ok(new { message = "User account deleted." });
        });
    }
}

public class AssignBranchRequestDto
{
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
}

public class UpdateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    public string NewPassword { get; set; } = string.Empty;
}