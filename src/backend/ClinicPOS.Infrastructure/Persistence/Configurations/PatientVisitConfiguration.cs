using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicPOS.Infrastructure.Persistence.Configurations;

public class PatientVisitConfiguration : IEntityTypeConfiguration<PatientVisit>
{
    public void Configure(EntityTypeBuilder<PatientVisit> builder)
    {
        builder.ToTable("patient_visits");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(v => v.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(v => v.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(v => v.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(v => v.VisitedAt).HasColumnName("visited_at").IsRequired();
        builder.Property(v => v.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(v => new { v.TenantId, v.PatientId, v.BranchId, v.VisitedAt })
            .IsUnique();

        builder.HasOne(v => v.Patient).WithMany().HasForeignKey(v => v.PatientId);
        builder.HasOne(v => v.Branch).WithMany().HasForeignKey(v => v.BranchId);
    }
}
