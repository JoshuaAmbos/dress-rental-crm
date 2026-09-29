using CRM.api.DTOs;
using CRM.domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CRM.api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // 1. AUTHENTICATION (/api/auth)
        var authGroup = routes.MapGroup("/api/auth");

        authGroup.MapPost("/login", async (LoginRequestDto request, UserManager<IdentityUser> userManager) =>
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

            return Results.Ok(new LoginResult
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CompanyId = companyId,
                CompanyName = companyName,
                Roles = roles.ToArray()
            });
        });

        // 2. USER MANAGEMENT CRUD (/api/users)
        var userGroup = routes.MapGroup("/api/users");

        // CREATE
        userGroup.MapPost("/register", async (
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
        });

        // UPDATE (Role, Email, Tenant)
        userGroup.MapPut("/{userId}", async (
            string userId,
            UpdateUserRequestDto request,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            if (user.UserName?.Equals("superadmin", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.BadRequest(new { message = "Superadmin account cannot be modified." });
            }

            // Update Email
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                user.Email = request.Email.Trim();
                await userManager.UpdateAsync(user);
            }

            // Update Role
            if (!string.IsNullOrWhiteSpace(request.Role) && new[] { AppRoles.Staff, AppRoles.Manager, AppRoles.Admin }.Contains(request.Role))
            {
                var currentRoles = await userManager.GetRolesAsync(user);
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                await userManager.AddToRoleAsync(user, request.Role);
            }

            // Update Tenant Claims
            var claims = await userManager.GetClaimsAsync(user);
            var oldTenantClaims = claims.Where(c => c.Type == "CompanyId" || c.Type == "CompanyName").ToList();
            foreach (var c in oldTenantClaims) await userManager.RemoveClaimAsync(user, c);

            await userManager.AddClaimAsync(user, new Claim("CompanyId", request.CompanyId.ToString()));
            await userManager.AddClaimAsync(user, new Claim("CompanyName", request.CompanyName));

            return Results.Ok(new { message = "User updated successfully." });
        });

        // RESET PASSWORD
        userGroup.MapPost("/{userId}/reset-password", async (
            string userId,
            ResetPasswordRequestDto request,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$");
            if (!passwordRegex.IsMatch(request.NewPassword))
            {
                return Results.BadRequest(new { message = "Password must be at least 8 chars, with 1 uppercase, 1 lowercase, 1 digit, and 1 special char." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            return Results.Ok(new { message = "Password reset successfully." });
        });

        // DELETE
        userGroup.MapDelete("/{userId}", async (
            string userId,
            UserManager<IdentityUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            if (user.UserName?.Equals("superadmin", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.BadRequest(new { message = "Cannot delete the Superadmin account." });
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            return Results.Ok(new { message = "User account removed." });
        });
    }
}

// Supporting DTOs
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