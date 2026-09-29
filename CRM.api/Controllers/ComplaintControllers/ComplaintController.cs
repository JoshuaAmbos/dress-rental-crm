using CRM.api.DTOs;
using CRM.api.Services;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.api.Controllers;

public class ComplaintController
{
    private readonly ComplaintService _complaintService;

    public ComplaintController(Func<TenantCrmDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _complaintService = new ComplaintService(contextFactory);
    }

    public async Task<ComplaintPipelineDto> LoadPipelineAsync(int companyId, int? branchId = null, string statusFilter = "All", string search = "")
    {
        return await _complaintService.GetPipelineAsync(companyId, branchId, statusFilter, search);
    }

    public async Task<Complaint?> GetComplaintAsync(int complaintId)
    {
        return await _complaintService.GetComplaintAsync(complaintId);
    }

    public async Task SaveComplaintAsync(Complaint model)
    {
        await _complaintService.SaveComplaintAsync(model);
    }

    public async Task ResolveComplaintAsync(int complaintId, string notes, decimal compensation)
    {
        await _complaintService.ResolveComplaintAsync(complaintId, notes, compensation);
    }
}