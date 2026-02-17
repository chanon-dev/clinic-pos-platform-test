using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicPOS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.Role)
            .HasConversion(r => r.ToString(), r => Enum.Parse<Role>(r))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("uq_users_username");
        builder.HasIndex(u => u.TenantId).HasDatabaseName("ix_users_tenant_id");

        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(u => u.TenantId);
    }
}
