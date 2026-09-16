using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.winforms.Models;

namespace CRM.winforms.Services;

public record AuthResponse(bool Success, string Message, LoginResult? User);

public class AuthService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5171/")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        var cleanUsername = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleanUsername) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthResponse(false, "Please enter both username and password.", null);
        }

        try
        {
            var payload = new { Username = cleanUsername, Password = password };
            var response = await Http.PostAsJsonAsync("/api/auth/login", payload);

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions);
                return new AuthResponse(true, string.Empty, user);
            }

            return new AuthResponse(false, "Invalid username or password.", null);
        }
        catch
        {
            return new AuthResponse(false, "Connection failed. Is the API online?", null);
        }
    }
}