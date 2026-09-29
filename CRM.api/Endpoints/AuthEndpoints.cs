using System.Security.Claims;
using System.Text.RegularExpressions;
using CRM.api.DTOs;
using CRM.domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace CRM.api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // 1. Auth Group
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

        // 2. User Registration Handler (Mapped on BOTH routes to prevent 404s)
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

        // Registered under both paths
        routes.MapPost("/api/users/register", registerDelegate);
        routes.MapPost("/api/auth/register", registerDelegate);
    }
}