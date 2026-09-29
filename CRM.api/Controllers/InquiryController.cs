using CRM.api.DTOs;
using CRM.api.Services;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.api.Controllers;

public class InquiryController
{
    private readonly InquiryService _inquiryService;

    public InquiryController(Func<TenantCrmDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _inquiryService = new InquiryService(contextFactory);
    }

    public Task<InquiryPipelineDto> LoadPipelineAsync(int companyId, string statusFilter = "All", string search = "")
        => _inquiryService.GetPipelineAsync(companyId, null, statusFilter, search);

    public Task<InquiryPipelineDto> LoadPipelineAsync(int companyId, int? branchId, string statusFilter = "All", string search = "")
    {
        return _inquiryService.GetPipelineAsync(companyId, branchId, statusFilter, search);
    }

    public Task<Inquiry?> GetInquiryAsync(int inquiryId)
    {
        return _inquiryService.GetInquiryAsync(inquiryId);
    }

    public Task SaveInquiryAsync(Inquiry model)
    {
        return _inquiryService.SaveInquiryAsync(model);
    }

    public Task MarkConvertedAsync(int inquiryId)
    {
        return _inquiryService.MarkConvertedAsync(inquiryId);
    }
}