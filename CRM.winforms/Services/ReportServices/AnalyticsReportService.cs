using CRM.infrastructure.data;
using CRM.winforms.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Services;

// pulls the tenant's bookings and computes the KPI metrics,
// 6-month revenue progression, stage distributions,
// and top-leased gowns in memory

public class AnalyticsReportService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public AnalyticsReportService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<AnalyticsDashboardDto> GetAnalyticsOverviewAsync(int companyId, int monthsLookback = 6)
    {
        await using var db = _contextFactory();
        var today = DateTime.Today;
        var lookbackStart = new DateTime(today.Year, today.Month, 1).AddMonths(-(monthsLookback - 1));

        // 1. Fetch bookings in scope
        var bookings = await db.RentalBookings
            .AsNoTracking()
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Garment)
            .Where(b => b.CompanyId == companyId)
            .ToListAsync();

        // Helper to test if a stage is "Returned"
        static bool IsReturned(string? stage) =>
            string.Equals(stage?.Trim(), "Returned", StringComparison.OrdinalIgnoreCase);

        // Helper to test if a stage is "Cancelled"
        static bool IsCancelled(string? stage) =>
            string.Equals(stage?.Trim(), "Cancelled", StringComparison.OrdinalIgnoreCase);

        // 2. High-Level KPIs
        // Active circulation includes ONLY bookings that are not returned and not cancelled
        var activeCirculationBookings = bookings
            .Where(b => !IsReturned(b.BookingStage) && !IsCancelled(b.BookingStage))
            .ToList();

        var completedBookings = bookings
            .Where(b => IsReturned(b.BookingStage))
            .ToList();

        decimal totalLeaseRevenue = bookings
            .Where(b => !IsCancelled(b.BookingStage))
            .Sum(b => b.RentalFee);

        // Security deposits held only apply to items genuinely still out in circulation
        decimal activeDepositsHeld = activeCirculationBookings.Sum(b => b.SecurityDeposit);

        int totalCompleted = completedBookings.Count;
        int onTimeReturns = completedBookings.Count(b => b.RentalEndDate.Date >= b.CreatedAt.Date);
        double returnRate = totalCompleted > 0 ? (double)onTimeReturns / totalCompleted * 100.0 : 100.0;

        // 3. Monthly Revenue Trend (Last N Months)
        var monthlyTrend = new List<MonthlyRevenueMetric>();
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

        // 4. Stage Breakdown Distribution
        var stageGroups = bookings
            .Where(b => !string.IsNullOrWhiteSpace(b.BookingStage) && !IsCancelled(b.BookingStage))
            .GroupBy(b =>
            {
                // CRITICAL: If already returned, it CANNOT be overdue
                if (IsReturned(b.BookingStage))
                {
                    return "Returned";
                }

                // If not returned and end date passed, it's overdue
                if (b.RentalEndDate.Date < today)
                {
                    return "Overdue";
                }

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

        // 5. Top Leased Garments Leaderboard
        var garmentDetails = bookings
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

        return new AnalyticsDashboardDto
        {
            TotalLeaseRevenue = totalLeaseRevenue,
            ActiveDepositsHeld = activeDepositsHeld,
            TotalBookingsCompleted = totalCompleted,
            OnTimeReturnRate = Math.Round(returnRate, 1),
            MonthlyRevenueTrend = monthlyTrend,
            StageDistribution = stageDistribution,
            TopPerformingGarments = garmentDetails
        };
    }
}