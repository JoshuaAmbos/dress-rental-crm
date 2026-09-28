namespace CRM.winforms.Models;

public record UserSession(
    string UserId,
    string Username,
    string Email,
    int CompanyId,
    string CompanyName,
    IList<string> Roles)
{
    public string PrimaryRole => Roles.FirstOrDefault() ?? "Staff";
    public bool IsSuperAdmin => Roles.Contains("Superadmin");
}