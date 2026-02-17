using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filters for tenant isolation
        builder.Entity<Patient>()
            .HasQueryFilter(p => _tenantContext == null || p.TenantId == _tenantContext.TenantId);
        builder.Entity<Branch>()
            .HasQueryFilter(b => _tenantContext == null || b.TenantId == _tenantContext.TenantId);
        builder.Entity<Appointment>()
            .HasQueryFilter(a => _tenantContext == null || a.TenantId == _tenantContext.TenantId);
    }
}
