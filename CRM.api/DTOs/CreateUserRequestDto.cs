using CRM.domain.Constants;

namespace CRM.api.DTOs;

public class CreateUserRequestDto
{
    public string Username { get; set;} = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Staff;
}