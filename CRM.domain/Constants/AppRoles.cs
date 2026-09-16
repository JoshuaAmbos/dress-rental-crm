using System.Reflection.Metadata;

namespace CRM.domain.Constants;

public static class AppRoles
{
    public const string Staff = "Staff";
    public const string Manager = "Manager";
    public const string Admin = "Admin";
    public const string Superadmin = "Superadmin";

    public static readonly string[] AllRoles =
    {
        Staff,
        Manager,
        Admin,
        Superadmin  
    };
}