namespace CRM.api.DTOs;

public class BranchDto
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int TotalGarments { get; set; }
    public int ActiveRentals { get; set; }
}