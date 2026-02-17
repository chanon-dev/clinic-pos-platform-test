using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicPOS.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.TenantId, p.PhoneNumber })
            .IsUnique()
            .HasDatabaseName("uq_patients_tenant_phone");
        builder.HasIndex(p => p.TenantId).HasDatabaseName("ix_patients_tenant_id");
        builder.HasIndex(p => new { p.TenantId, p.PrimaryBranchId })
            .HasDatabaseName("ix_patients_tenant_branch");
        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_patients_created_at");

        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(p => p.TenantId);
        builder.HasOne(p => p.PrimaryBranch).WithMany()
            .HasForeignKey(p => p.PrimaryBranchId)
            .IsRequired(false);
    }
}
