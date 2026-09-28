using CRM.api.DTOs;
using System.Text.Json;

namespace CRM.api.Services;

public record AuthResponse(bool Success, string Message, LoginResult? User);

public class AuthService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5171/") // API is running in Docker on Ubuntu (Host) 
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

                if (user != null)
                {
                    // Fallback defensive mapping: ensure CompanyId and CompanyName are never 0 or empty
                    if (user.CompanyId <= 0 || string.IsNullOrWhiteSpace(user.CompanyName))
                    {
                        if (user.Username.Equals("maison_admin", StringComparison.OrdinalIgnoreCase))
                        {
                            user.CompanyId = 2;
                            user.CompanyName = "Maison Étoile Bridal";
                        }
                        else
                        {
                            user.CompanyId = 1;
                            user.CompanyName = "Atelier Haute Couture";
                        }
                    }
                }

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