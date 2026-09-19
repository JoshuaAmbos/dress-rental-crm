using CRM.domain.entities;

namespace CRM.winforms.Models;

public class CustomerRowViewModel
{
    public Customer CustomerEntity { get; set; } = null!;
    public int CustomerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BustSize { get; set; } = string.Empty;
    public string WaistSize { get; set; } = string.Empty;
    public string HipSize { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}