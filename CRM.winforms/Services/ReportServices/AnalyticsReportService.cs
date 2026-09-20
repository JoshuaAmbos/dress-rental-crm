using CRM.infrastructure.data;
using CRM.winforms.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Services;

public class AnalyticsReportService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public AnalyticsReportService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<AnalyticsDashboardDto> GetAnalyticsOverviewAsync(int companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var db = _contextFactory();
        var today = DateTime.Today;

        // 1. Fetch total wardrobe fleet for utilization metric
        var allGarments = await db.Garments
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId && g.IsActive)
            .ToListAsync();

        int totalActiveFleet = allGarments.Count;
        int currentlyRentedCount = allGarments.Count(g => string.Equals(g.Status, "Rented", StringComparison.OrdinalIgnoreCase));
        double fleetUtilization = totalActiveFleet > 0
            ? Math.Round((double)currentlyRentedCount / totalActiveFleet * 100.0, 1)
            : 0.0;

        // 2. Fetch bookings query
        var query = db.RentalBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .Where(b => b.CompanyId == companyId);

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(b => b.RentalStartDate >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(b => b.RentalStartDate <= end);
        }

        var bookings = await query.ToListAsync();

        static bool IsReturned(string? stage) =>
            string.Equals(stage?.Trim(), "Returned", StringComparison.OrdinalIgnoreCase);

        static bool IsCancelled(string? stage) =>
            string.Equals(stage?.Trim(), "Cancelled", StringComparison.OrdinalIgnoreCase);

        // 3. High-Level KPIs
        var activeCirculationBookings = bookings
            .Where(b => !IsReturned(b.BookingStage) && !IsCancelled(b.BookingStage))
            .ToList();

        var completedBookings = bookings
            .Where(b => IsReturned(b.BookingStage))
            .ToList();

        decimal totalLeaseRevenue = bookings
            .Where(b => !IsCancelled(b.BookingStage))
            .Sum(b => b.RentalFee);

        decimal activeDepositsHeld = activeCirculationBookings.Sum(b => b.SecurityDeposit);

        int totalCompleted = completedBookings.Count;
        int onTimeReturns = completedBookings.Count(b => b.RentalEndDate.Date >= b.CreatedAt.Date);
        double returnRate = totalCompleted > 0 ? (double)onTimeReturns / totalCompleted * 100.0 : 100.0;

        // 4. Monthly Trend
        var monthlyTrend = new List<MonthlyRevenueMetric>();
        int monthsLookback = 6;
        var lookbackStart = new DateTime(today.Year, today.Month, 1).AddMonths(-(monthsLookback - 1));

        for (int i = 0; i < monthsLookback; i++)
        {
            var targetMonth = lookbackStart.AddMonths(i);
            var monthBookings = bookings.Where(b =>
                !IsCancelled(b.BookingStage) &&
                b.RentalStartDate.Year == targetMonth.Year &&
                b.RentalStartDate.Month == targetMonth.Month).ToList();

            monthlyTrend.Add(new MonthlyRevenueMetric
            {
                MonthLabel = targetMonth.ToString("MMM yyyy"),
                Revenue = monthBookings.Sum(b => b.RentalFee),
                BookingCount = monthBookings.Count
            });
        }

        // 5. Stage Breakdown Distribution
        var stageGroups = bookings
            .Where(b => !string.IsNullOrWhiteSpace(b.BookingStage) && !IsCancelled(b.BookingStage))
            .GroupBy(b =>
            {
                if (IsReturned(b.BookingStage)) return "Returned";
                if (b.RentalEndDate.Date < today) return "Overdue";

                string raw = b.BookingStage.Trim();
                return char.ToUpper(raw[0]) + raw.Substring(1).ToLower();
            })
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToList();

        int totalRelevantBookings = bookings.Count(b => !IsCancelled(b.BookingStage));
        var stageDistribution = stageGroups.Select(sg => new StageDistributionMetric
        {
            StageName = sg.Stage,
            Count = sg.Count,
            Percentage = totalRelevantBookings > 0 ? Math.Round((double)sg.Count / totalRelevantBookings * 100.0, 1) : 0
        }).OrderByDescending(s => s.Count).ToList();

        // 6. Top Leased Garments Leaderboard
        var topGarments = bookings
            .Where(b => !IsCancelled(b.BookingStage))
            .SelectMany(b => b.BookingDetails)
            .Where(d => d.Garment != null)
            .GroupBy(d => d.GarmentId)
            .Select(g => new TopGarmentReportRow
            {
                GarmentId = g.Key,
                ItemCode = g.First().Garment!.ItemCode,
                StyleName = g.First().Garment!.StyleName,
                Size = string.IsNullOrWhiteSpace(g.First().Garment!.Size) ? "Standard" : g.First().Garment!.Size,
                TotalRentals = g.Count(),
                TotalRevenueGenerated = g.Sum(d => d.UnitPrice)
            })
            .OrderByDescending(r => r.TotalRentals)
            .ThenByDescending(r => r.TotalRevenueGenerated)
            .Take(5)
            .ToList();

        // 7. Audit Ledger Table Rows
        var auditLedger = bookings
            .OrderByDescending(b => b.RentalStartDate)
            .Select(b => new RentalLedgerRowDto
            {
                BookingId = b.RentalBookingId,
                BookingCode = $"BKG-{b.RentalBookingId:D4}",
                ClientName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}".Trim() : "Client Profile",
                GarmentSummary = b.BookingDetails.Count > 0
                    ? string.Join(", ", b.BookingDetails.Select(d => d.Garment != null ? d.Garment.StyleName : $"Garment #{d.GarmentId}"))
                    : "Unassigned",
                RentalStartDate = b.RentalStartDate,
                RentalEndDate = b.RentalEndDate,
                RentalFee = b.RentalFee,
                SecurityDeposit = b.SecurityDeposit,
                Stage = b.BookingStage,
                PaymentMethod = string.IsNullOrWhiteSpace(b.PaymentMethod) ? "Standard" : b.PaymentMethod
            })
            .ToList();

        return new AnalyticsDashboardDto
        {
            TotalLeaseRevenue = totalLeaseRevenue,
            ActiveDepositsHeld = activeDepositsHeld,
            TotalBookingsCompleted = totalCompleted,
            OnTimeReturnRate = Math.Round(returnRate, 1),
            FleetUtilizationRate = fleetUtilization,
            TotalActiveGarments = totalActiveFleet,
            CurrentlyRentedGarments = currentlyRentedCount,
            MonthlyRevenueTrend = monthlyTrend,
            StageDistribution = stageDistribution,
            TopPerformingGarments = topGarments,
            AuditLedger = auditLedger
        };
    }
}