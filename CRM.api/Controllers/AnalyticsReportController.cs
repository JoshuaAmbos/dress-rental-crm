using CRM.infrastructure.data;
using CRM.api.DTOs;
using CRM.api.Services;

namespace CRM.api.Controllers;

public class AnalyticsReportController
{
    private readonly AnalyticsReportService _analyticsService;

    public AnalyticsReportController(Func<TenantCrmDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _analyticsService = new AnalyticsReportService(contextFactory);
    }

    public async Task<AnalyticsDashboardDto> LoadDashboardMetricsAsync(int companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        return await _analyticsService.GetAnalyticsOverviewAsync(companyId, startDate, endDate);
    }
}