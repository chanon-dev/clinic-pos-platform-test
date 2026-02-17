using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicPOS.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.StartAt).IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        // Prevent double-booking: same patient, branch, and time within a tenant
        builder.HasIndex(a => new { a.TenantId, a.BranchId, a.PatientId, a.StartAt })
            .IsUnique()
            .HasDatabaseName("uq_appointments_tenant_branch_patient_start");

        builder.HasIndex(a => a.TenantId).HasDatabaseName("ix_appointments_tenant_id");
        builder.HasIndex(a => new { a.TenantId, a.BranchId })
            .HasDatabaseName("ix_appointments_tenant_branch");
        builder.HasIndex(a => a.StartAt)
            .HasDatabaseName("ix_appointments_start_at");

        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(a => a.TenantId);
        builder.HasOne(a => a.Branch).WithMany()
            .HasForeignKey(a => a.BranchId);
        builder.HasOne(a => a.Patient).WithMany()
            .HasForeignKey(a => a.PatientId);
    }
}
