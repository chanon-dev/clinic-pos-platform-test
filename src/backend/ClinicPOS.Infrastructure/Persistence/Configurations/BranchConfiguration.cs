using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicPOS.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(b => b.TenantId).HasDatabaseName("ix_branches_tenant_id");

        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(b => b.TenantId);
    }
}
