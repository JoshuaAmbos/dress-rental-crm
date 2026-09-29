using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRM.winforms.Services;

public class UserAccountService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5171/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<(bool Success, string Message)> RegisterUserAsync(
        string username, string email, string password, string role, int companyId, string companyName)
    {
        try
        {
            var payload = new
            {
                Username = username,
                Email = email,
                Password = password,
                Role = role,
                CompanyId = companyId,
                CompanyName = companyName
            };

            // Note the leading slash: /api/users/register
            var response = await Http.PostAsJsonAsync("/api/users/register", payload);
            return await ParseResponseAsync(response, "User registered successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.GetBaseException().Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(
        string userId, string email, string role, int companyId, string companyName)
    {
        try
        {
            var payload = new
            {
                Email = email,
                Role = role,
                CompanyId = companyId,
                CompanyName = companyName
            };

            var response = await Http.PutAsJsonAsync($"/api/users/{userId}", payload);
            return await ParseResponseAsync(response, "User updated successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.GetBaseException().Message}");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string userId, string newPassword)
    {
        try
        {
            var payload = new { NewPassword = newPassword };
            var response = await Http.PostAsJsonAsync($"/api/users/{userId}/reset-password", payload);
            return await ParseResponseAsync(response, "Password reset successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.GetBaseException().Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        try
        {
            var response = await Http.DeleteAsync($"/api/users/{userId}");
            return await ParseResponseAsync(response, "User deleted successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.GetBaseException().Message}");
        }
    }

    private static async Task<(bool Success, string Message)> ParseResponseAsync(HttpResponseMessage response, string successMessage)
    {
        if (response.IsSuccessStatusCode)
        {
            return (true, successMessage);
        }

        var rawError = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(rawError);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return (false, msg.GetString() ?? "Operation rejected.");
            }
        }
        catch { }

        return (false, $"Rejected ({response.StatusCode}): {rawError}");
    }
}