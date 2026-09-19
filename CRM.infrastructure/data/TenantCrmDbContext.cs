using CRM.domain.entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CRM.infrastructure.data;

public class TenantCrmDbContext : DbContext
{
    public TenantCrmDbContext(DbContextOptions<TenantCrmDbContext> options)
        : base(options)
    {
    }

    public DbSet<Garment> RentalItems => Set<Garment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RentalBooking> RentalBookings => Set<RentalBooking>();
    public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();
    public DbSet<ServiceIncident> ServiceIncidents => Set<ServiceIncident>();
    public DbSet<LoyaltyAward> LoyaltyAwards => Set<LoyaltyAward>();
    public DbSet<Garment> Garments { get; set; } = null!;
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Garment
        builder.Entity<Garment>(entity =>
        {
            entity.HasKey(e => e.GarmentId);
            entity.Property(e => e.RentalRate).HasPrecision(18, 2);
            entity.Property(e => e.SecurityDeposit).HasPrecision(18, 2);
            entity.Property(e => e.ReplacementValue).HasPrecision(18, 2);
            entity.Property(e => e.BustSize).HasPrecision(5, 2);
            entity.Property(e => e.WaistSize).HasPrecision(5, 2);
            entity.Property(e => e.HipSize).HasPrecision(5, 2);
        });

        // Customer
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.CustomerId);
            entity.Property(x => x.CustomerCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MiddleName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Ignore(x => x.FullName);
            entity.Property(x => x.ContactNumber).HasMaxLength(30);
            entity.Property(x => x.EmailAddress).HasMaxLength(150);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.BustSize).HasPrecision(5, 2);
            entity.Property(x => x.WaistSize).HasPrecision(5, 2);
            entity.Property(x => x.HipSize).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.CompanyId, x.CustomerCode }).IsUnique();
        });

        // RentalBooking
        builder.Entity<RentalBooking>(entity =>
        {
            entity.ToTable("RentalBookings");

            entity.HasKey(x => x.RentalBookingId);

            entity.Property(x => x.BookingStage)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(x => x.RentalFee)
                  .HasPrecision(18, 2);

            entity.Property(x => x.SecurityDeposit)
                  .HasPrecision(18, 2);

            entity.Property(x => x.AlterationNotes)
                  .HasMaxLength(500);

            // Relationship to Customer
            entity.HasOne(x => x.Customer)
                  .WithMany(x => x.RentalBookings)
                  .HasForeignKey(x => x.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 1-to-Many Relationship to BookingDetail (Multi-Item)
            entity.HasMany(x => x.BookingDetails)
                  .WithOne(x => x.RentalBooking)
                  .HasForeignKey(x => x.RentalBookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BookingDetail
        builder.Entity<BookingDetail>(entity =>
        {
            entity.ToTable("BookingDetails");

            entity.HasKey(x => x.BookingDetailId);

            entity.Property(x => x.AlterationNotes)
                  .HasMaxLength(500);

            // If BookingDetail tracks line-item rental pricing:
            entity.Property(x => x.UnitPrice)
                  .HasPrecision(18, 2);

            // Many-to-1 Relationship to Garment
            entity.HasOne(x => x.Garment)
                  .WithMany(x => x.BookingDetails)
                  .HasForeignKey(x => x.GarmentId)
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