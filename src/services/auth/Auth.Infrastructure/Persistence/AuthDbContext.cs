using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
        });
    }
}
