using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;

namespace CRM.infrastructure.data;

public class MasterCrmDbContext : IdentityDbContext
{
    public MasterCrmDbContext(DbContextOptions<MasterCrmDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
    public DbSet<SubscriptionPackage> SubscriptionPackages => Set<SubscriptionPackage>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SubscriptionPackage Configuration
        builder.Entity<SubscriptionPackage>(entity =>
        {
            entity.HasKey(x => x.SubscriptionPackageId);
            entity.Property(x => x.PackageName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MonthlyFee).HasPrecision(18, 2);
        });

        // Company Configuration
        builder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.CompanyId);
            entity.Property(x => x.CompanyCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.CompanyCode).IsUnique();

            entity.HasOne(x => x.SubscriptionPackage)
                  .WithMany(p => p.Companies)
                  .HasForeignKey(x => x.SubscriptionPackageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CompanyDatabase Configuration
        builder.Entity<CompanyDatabase>(entity =>
        {
            entity.HasKey(x => x.CompanyDatabaseId);
            entity.Property(x => x.ServerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CredentialKey).HasMaxLength(100).IsRequired();

            entity.HasOne(x => x.Company)
                  .WithMany(c => c.CompanyDatabases)
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Device Configuration
        builder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.DeviceId);
            entity.Property(x => x.DeviceCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.Company)
                  .WithMany(x => x.Devices)
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.CompanyId, x.DeviceCode }).IsUnique();
        });
    }
}