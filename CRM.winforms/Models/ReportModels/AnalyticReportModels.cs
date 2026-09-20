using System.Collections.Generic;

namespace CRM.winforms.Models;

public class MonthlyRevenueMetric
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int BookingCount { get; set; }
}

public class StageDistributionMetric
{
    public string StageName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TopGarmentReportRow
{
    public int GarmentId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int TotalRentals { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
}

public class AnalyticsDashboardDto
{
    // High-Level KPIs
    public decimal TotalLeaseRevenue { get; set; }
    public decimal ActiveDepositsHeld { get; set; }
    public int TotalBookingsCompleted { get; set; }
    public double OnTimeReturnRate { get; set; }

    // Chart Series
    public List<MonthlyRevenueMetric> MonthlyRevenueTrend { get; set; } = [];
    public List<StageDistributionMetric> StageDistribution { get; set; } = [];
    public List<TopGarmentReportRow> TopPerformingGarments { get; set; } = [];
}