using System;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Auth.Infrastructure.Migrations;

[DbContext(typeof(AuthDbContext))]
partial class AuthDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("Auth.Domain.Entities.UserEntity", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            entity.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            entity.Property<string>("Email").IsRequired().HasMaxLength(320).HasColumnName("email");
            entity.Property<string>("Name").HasMaxLength(200).HasColumnName("name");
            entity.Property<string>("NormalizedEmail").IsRequired().HasMaxLength(320).HasColumnName("normalized_email");
            entity.Property<string>("PasswordHash").HasColumnName("password_hash");
            entity.Property<DateTime>("UpdatedAt").HasColumnName("updated_at");

            entity.HasKey("Id");
            entity.HasIndex("NormalizedEmail").IsUnique();
            entity.ToTable("users");
        });
    }
}
