using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;

namespace CRM.infrastructure.data;

public class TenantCrmDbContext : DbContext
{
    public TenantCrmDbContext(DbContextOptions<TenantCrmDbContext> options)
        : base(options)
    {
    }

    public DbSet<RentalItem> RentalItems => Set<RentalItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RentalBooking> RentalBookings => Set<RentalBooking>();
    public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();
    public DbSet<ServiceIncident> ServiceIncidents => Set<ServiceIncident>();
    public DbSet<LoyaltyAward> LoyaltyAwards => Set<LoyaltyAward>();
    public DbSet<RentalTerm> RentalTerms => Set<RentalTerm>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // RentalItem
        builder.Entity<RentalItem>(entity =>
        {
            entity.HasKey(x => x.RentalItemId);
            entity.Property(x => x.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.StyleName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RentalRate).HasPrecision(18, 2);
            entity.Property(x => x.ReplacementValue).HasPrecision(18, 2);
            entity.HasIndex(x => x.ItemCode).IsUnique();
        });

        // Customer
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.CustomerId);
            entity.Property(x => x.CustomerCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30);
            entity.Property(x => x.EmailAddress).HasMaxLength(150);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.BustSize).HasPrecision(5, 2);
            entity.Property(x => x.WaistSize).HasPrecision(5, 2);
            entity.Property(x => x.HipSize).HasPrecision(5, 2);
            entity.HasIndex(x => x.CustomerCode).IsUnique();
        });

        // RentalBooking
        builder.Entity<RentalBooking>(entity =>
        {
            entity.HasKey(x => x.RentalBookingId);
            entity.Property(x => x.DressDescription).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BookingStage).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RentalFee).HasPrecision(18, 2);
            entity.Property(x => x.SecurityDeposit).HasPrecision(18, 2);

            entity.HasOne(x => x.Customer)
                  .WithMany(x => x.RentalBookings)
                  .HasForeignKey(x => x.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BookingDetail
        builder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(x => x.BookingDetailId);
            entity.Property(x => x.AlterationNotes).HasMaxLength(500);

            entity.HasOne(x => x.RentalBooking)
                  .WithMany(x => x.BookingDetails)
                  .HasForeignKey(x => x.RentalBookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.RentalItem)
                  .WithMany(x => x.BookingDetails)
                  .HasForeignKey(x => x.RentalItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ServiceIncident
        builder.Entity<ServiceIncident>(entity =>
        {
            entity.HasKey(x => x.ServiceIncidentId);
            entity.Property(x => x.IncidentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FeeCharged).HasPrecision(18, 2);
            entity.Property(x => x.ResolutionNotes).HasMaxLength(1000);

            entity.HasOne(x => x.RentalBooking)
                  .WithMany(x => x.ServiceIncidents)
                  .HasForeignKey(x => x.RentalBookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LoyaltyAward
        builder.Entity<LoyaltyAward>(entity =>
        {
            entity.HasKey(x => x.LoyaltyAwardId);
            entity.Property(x => x.TierName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MinLifetimeSpend).HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(x => x.RewardDescription).HasMaxLength(250).IsRequired();
        });

        // RentalTerm
        builder.Entity<RentalTerm>(entity =>
        {
            entity.HasKey(x => x.RentalTermId);
            entity.Property(x => x.PolicyTitle).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PolicyContent).IsRequired();
            entity.Property(x => x.VersionNumber).HasMaxLength(20).IsRequired();
        });

        // SystemConfiguration
        builder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasKey(x => x.SystemConfigurationId);
            entity.Property(x => x.ConfigKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ConfigValue).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(250);
            entity.HasIndex(x => x.ConfigKey).IsUnique();
        });
    }
}