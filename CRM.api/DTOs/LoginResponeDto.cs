namespace CRM.api.DTOs;

public class LoginResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}