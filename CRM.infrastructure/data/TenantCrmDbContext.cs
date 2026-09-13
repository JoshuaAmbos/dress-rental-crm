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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RentalItem>(entity =>
        {
            entity.HasKey(x => x.RentalItemId);

            entity.Property(x => x.ItemCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.StyleName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.RentalRate)
                .HasPrecision(18, 2);

            entity.Property(x => x.ReplacementValue)
                .HasPrecision(18, 2);

            entity.HasIndex(x => x.ItemCode)
                .IsUnique();
        });
    }
}