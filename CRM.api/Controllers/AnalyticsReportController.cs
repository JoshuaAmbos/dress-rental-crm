using CRM.api.DTOs;
using CRM.api.Services;
using CRM.infrastructure.data;

namespace CRM.api.Controllers;

public class AnalyticsReportController
{
    private readonly AnalyticsReportService _analyticsService;

    public AnalyticsReportController(Func<TenantCrmDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _analyticsService = new AnalyticsReportService(contextFactory);
    }

    public Task<AnalyticsDashboardDto> LoadDashboardMetricsAsync(int companyId, DateTime? startDate = null, DateTime? endDate = null)
        => LoadDashboardMetricsAsync(companyId, null, startDate, endDate);

    public async Task<AnalyticsDashboardDto> LoadDashboardMetricsAsync(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        return await _analyticsService.GetAnalyticsOverviewAsync(companyId, branchId, startDate, endDate);
    }
}