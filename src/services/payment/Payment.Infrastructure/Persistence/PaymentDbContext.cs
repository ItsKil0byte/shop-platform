using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.TransactionId);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Индекс для быстрого поиска по OrderId (нужен для идемпотентности)
            entity.HasIndex(e => e.OrderId).IsUnique();
        });
    }
}
