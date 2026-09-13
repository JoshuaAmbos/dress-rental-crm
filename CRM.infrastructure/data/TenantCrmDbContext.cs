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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // RentalItem mapping
        builder.Entity<RentalItem>(entity =>
        {
            entity.HasKey(x => x.RentalItemId);
            entity.Property(x => x.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.StyleName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RentalRate).HasPrecision(18, 2);
            entity.Property(x => x.ReplacementValue).HasPrecision(18, 2);
            entity.HasIndex(x => x.ItemCode).IsUnique();
        });

        // Customer mapping
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.CustomerId);

            entity.Property(x => x.CustomerCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.CustomerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.ContactNumber).HasMaxLength(30);
            entity.Property(x => x.EmailAddress).HasMaxLength(150);
            entity.Property(x => x.Address).HasMaxLength(300);

            entity.Property(x => x.BustSize).HasPrecision(5, 2);
            entity.Property(x => x.WaistSize).HasPrecision(5, 2);
            entity.Property(x => x.HipSize).HasPrecision(5, 2);

            entity.HasIndex(x => x.CustomerCode).IsUnique();
        });
    }
}