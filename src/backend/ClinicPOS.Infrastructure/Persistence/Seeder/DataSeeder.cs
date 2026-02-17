using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence.Seeder;

public static class DataSeeder
{
    public static readonly Guid TenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Branch1Id = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Branch2Id = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid AdminId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid UserId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    public static readonly Guid ViewerId = Guid.Parse("c0000000-0000-0000-0000-000000000003");

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync())
            return;

        var tenant = new Tenant
        {
            Id = TenantId,
            Name = "คลินิกสุขภาพดี",
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);

        var branch1 = new Branch { Id = Branch1Id, TenantId = TenantId, Name = "สาขาสยาม", CreatedAt = DateTime.UtcNow };
        var branch2 = new Branch { Id = Branch2Id, TenantId = TenantId, Name = "สาขาทองหล่อ", CreatedAt = DateTime.UtcNow };
        db.Branches.AddRange(branch1, branch2);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd");

        var admin = new User
        {
            Id = AdminId, TenantId = TenantId, Username = "admin01",
            PasswordHash = passwordHash, Role = Role.Admin, CreatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = UserId, TenantId = TenantId, Username = "user01",
            PasswordHash = passwordHash, Role = Role.User, CreatedAt = DateTime.UtcNow
        };
        var viewer = new User
        {
            Id = ViewerId, TenantId = TenantId, Username = "viewer01",
            PasswordHash = passwordHash, Role = Role.Viewer, CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(admin, user, viewer);

        db.UserBranches.AddRange(
            new UserBranch { UserId = AdminId, BranchId = Branch1Id },
            new UserBranch { UserId = AdminId, BranchId = Branch2Id },
            new UserBranch { UserId = UserId, BranchId = Branch1Id },
            new UserBranch { UserId = ViewerId, BranchId = Branch1Id }
        );

        await db.SaveChangesAsync();
    }
}
