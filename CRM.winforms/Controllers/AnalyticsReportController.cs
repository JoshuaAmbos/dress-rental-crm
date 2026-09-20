using CRM.infrastructure.data;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Controllers;

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