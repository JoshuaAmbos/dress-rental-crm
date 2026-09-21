namespace CRM.winforms.Models;

public class AnalyticsDashboardDto
{
    public decimal TotalLeaseRevenue { get; set; }
    public decimal ActiveDepositsHeld { get; set; }
    public int TotalBookingsCompleted { get; set; }
    public double OnTimeReturnRate { get; set; }
    public double FleetUtilizationRate { get; set; }
    public int TotalActiveGarments { get; set; }
    public int CurrentlyRentedGarments { get; set; }

    public List<MonthlyRevenueMetric> MonthlyRevenueTrend { get; set; } = new();
    public List<StageDistributionMetric> StageDistribution { get; set; } = new();
    public List<TopGarmentReportRow> TopPerformingGarments { get; set; } = new();
    public List<TopCustomerReportRow> TopValuedCustomers { get; set; } = new();
    public List<RentalLedgerRowDto> AuditLedger { get; set; } = new();
}

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

public class TopCustomerReportRow
{
    public int CustomerId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public string ClientTier { get; set; } = "Standard";
}

public class RentalLedgerRowDto
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string GarmentSummary { get; set; } = string.Empty;
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public decimal RentalFee { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}