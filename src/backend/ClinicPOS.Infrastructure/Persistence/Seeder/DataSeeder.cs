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

    // Patient IDs
    private static readonly Guid Patient1Id = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid Patient2Id = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    private static readonly Guid Patient3Id = Guid.Parse("d0000000-0000-0000-0000-000000000003");
    private static readonly Guid Patient4Id = Guid.Parse("d0000000-0000-0000-0000-000000000004");
    private static readonly Guid Patient5Id = Guid.Parse("d0000000-0000-0000-0000-000000000005");
    private static readonly Guid Patient6Id = Guid.Parse("d0000000-0000-0000-0000-000000000006");
    private static readonly Guid Patient7Id = Guid.Parse("d0000000-0000-0000-0000-000000000007");
    private static readonly Guid Patient8Id = Guid.Parse("d0000000-0000-0000-0000-000000000008");
    private static readonly Guid Patient9Id = Guid.Parse("d0000000-0000-0000-0000-000000000009");
    private static readonly Guid Patient10Id = Guid.Parse("d0000000-0000-0000-0000-000000000010");

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // ── Tenant ──
        var tenant = new Tenant
        {
            Id = TenantId,
            Name = "คลินิกสุขภาพดี",
            CreatedAt = now
        };
        db.Tenants.Add(tenant);

        // ── Branches ──
        var branch1 = new Branch { Id = Branch1Id, TenantId = TenantId, Name = "สาขาสยาม", CreatedAt = now };
        var branch2 = new Branch { Id = Branch2Id, TenantId = TenantId, Name = "สาขาทองหล่อ", CreatedAt = now };
        db.Branches.AddRange(branch1, branch2);

        // ── Users ──
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd");

        var admin = new User
        {
            Id = AdminId,
            TenantId = TenantId,
            Username = "admin01",
            PasswordHash = passwordHash,
            Role = Role.Admin,
            CreatedAt = now
        };
        var user = new User
        {
            Id = UserId,
            TenantId = TenantId,
            Username = "user01",
            PasswordHash = passwordHash,
            Role = Role.User,
            CreatedAt = now
        };
        var viewer = new User
        {
            Id = ViewerId,
            TenantId = TenantId,
            Username = "viewer01",
            PasswordHash = passwordHash,
            Role = Role.Viewer,
            CreatedAt = now
        };
        db.Users.AddRange(admin, user, viewer);

        db.UserBranches.AddRange(
            new UserBranch { UserId = AdminId, BranchId = Branch1Id },
            new UserBranch { UserId = AdminId, BranchId = Branch2Id },
            new UserBranch { UserId = UserId, BranchId = Branch1Id },
            new UserBranch { UserId = ViewerId, BranchId = Branch1Id }
        );

        // ── Patients (10 records) ──
        var patients = new[]
        {
            new Patient { Id = Patient1Id,  TenantId = TenantId, FirstName = "สมชาย",   LastName = "วงศ์สว่าง",    PhoneNumber = "081-234-5671", PrimaryBranchId = Branch1Id, CreatedAt = now.AddDays(-60) },
            new Patient { Id = Patient2Id,  TenantId = TenantId, FirstName = "สมหญิง",  LastName = "รุ่งเรืองกิจ",  PhoneNumber = "089-876-5432", PrimaryBranchId = Branch1Id, CreatedAt = now.AddDays(-55) },
            new Patient { Id = Patient3Id,  TenantId = TenantId, FirstName = "วิชัย",    LastName = "ทองดี",        PhoneNumber = "062-111-2233", PrimaryBranchId = Branch2Id, CreatedAt = now.AddDays(-50) },
            new Patient { Id = Patient4Id,  TenantId = TenantId, FirstName = "นภา",     LastName = "ศรีสุข",       PhoneNumber = "095-444-5566", PrimaryBranchId = Branch1Id, CreatedAt = now.AddDays(-45) },
            new Patient { Id = Patient5Id,  TenantId = TenantId, FirstName = "ประยุทธ์",  LastName = "จันทร์แก้ว",    PhoneNumber = "086-777-8899", PrimaryBranchId = Branch2Id, CreatedAt = now.AddDays(-40) },
            new Patient { Id = Patient6Id,  TenantId = TenantId, FirstName = "มาลี",    LastName = "พิมพ์ทอง",     PhoneNumber = "091-222-3344", PrimaryBranchId = Branch1Id, CreatedAt = now.AddDays(-35) },
            new Patient { Id = Patient7Id,  TenantId = TenantId, FirstName = "ธนกร",    LastName = "อมรรัตน์",     PhoneNumber = "083-555-6677", PrimaryBranchId = Branch2Id, CreatedAt = now.AddDays(-30) },
            new Patient { Id = Patient8Id,  TenantId = TenantId, FirstName = "พิมพ์ใจ",  LastName = "สุวรรณภูมิ",    PhoneNumber = "064-888-9900", PrimaryBranchId = Branch1Id, CreatedAt = now.AddDays(-25) },
            new Patient { Id = Patient9Id,  TenantId = TenantId, FirstName = "อนุชา",   LastName = "เกษมสุข",      PhoneNumber = "097-333-4455", PrimaryBranchId = Branch2Id, CreatedAt = now.AddDays(-20) },
            new Patient { Id = Patient10Id, TenantId = TenantId, FirstName = "ปิยะ",    LastName = "มั่นคง",       PhoneNumber = "088-666-7788", PrimaryBranchId = Branch1Id, CreatedAt = now.AddDays(-15) },
        };
        db.Patients.AddRange(patients);

        // ── Appointments (8 records – mix of past & upcoming) ──
        var appointments = new[]
        {
            // Past appointments
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000001"), TenantId = TenantId, BranchId = Branch1Id, PatientId = Patient1Id,  StartAt = now.AddDays(-7).Date.AddHours(9),   CreatedAt = now.AddDays(-10) },
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000002"), TenantId = TenantId, BranchId = Branch1Id, PatientId = Patient2Id,  StartAt = now.AddDays(-5).Date.AddHours(10),  CreatedAt = now.AddDays(-8) },
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000003"), TenantId = TenantId, BranchId = Branch2Id, PatientId = Patient3Id,  StartAt = now.AddDays(-3).Date.AddHours(14),  CreatedAt = now.AddDays(-6) },
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000004"), TenantId = TenantId, BranchId = Branch1Id, PatientId = Patient4Id,  StartAt = now.AddDays(-1).Date.AddHours(11),  CreatedAt = now.AddDays(-4) },
            // Upcoming appointments
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000005"), TenantId = TenantId, BranchId = Branch1Id, PatientId = Patient6Id,  StartAt = now.AddDays(1).Date.AddHours(9),    CreatedAt = now.AddDays(-2) },
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000006"), TenantId = TenantId, BranchId = Branch2Id, PatientId = Patient7Id,  StartAt = now.AddDays(2).Date.AddHours(13),   CreatedAt = now.AddDays(-1) },
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000007"), TenantId = TenantId, BranchId = Branch1Id, PatientId = Patient8Id,  StartAt = now.AddDays(3).Date.AddHours(15),   CreatedAt = now },
            new Appointment { Id = Guid.Parse("e0000000-0000-0000-0000-000000000008"), TenantId = TenantId, BranchId = Branch2Id, PatientId = Patient5Id,  StartAt = now.AddDays(5).Date.AddHours(10),   CreatedAt = now },
        };
        db.Appointments.AddRange(appointments);

        // ── Patient Visits (12 records) ──
        var visits = new[]
        {
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000001"), TenantId = TenantId, PatientId = Patient1Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-30), Notes = "ตรวจสุขภาพประจำปี ผลตรวจปกติ",                           CreatedAt = now.AddDays(-30) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000002"), TenantId = TenantId, PatientId = Patient1Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-7),  Notes = "ติดตามผลเลือด ความดันปกติ 120/80",                    CreatedAt = now.AddDays(-7) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000003"), TenantId = TenantId, PatientId = Patient2Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-25), Notes = "ปวดหัวเรื้อรัง สั่งยาแก้ปวด",                          CreatedAt = now.AddDays(-25) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000004"), TenantId = TenantId, PatientId = Patient2Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-5),  Notes = "ติดตามอาการปวดหัว อาการดีขึ้น",                       CreatedAt = now.AddDays(-5) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000005"), TenantId = TenantId, PatientId = Patient3Id, BranchId = Branch2Id, VisitedAt = now.AddDays(-20), Notes = "ตรวจผิวหนัง ผื่นแพ้สัมผัส สั่งครีมทา",                  CreatedAt = now.AddDays(-20) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000006"), TenantId = TenantId, PatientId = Patient3Id, BranchId = Branch2Id, VisitedAt = now.AddDays(-3),  Notes = "ติดตามอาการผิวหนัง ผื่นหายแล้ว",                      CreatedAt = now.AddDays(-3) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000007"), TenantId = TenantId, PatientId = Patient4Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-15), Notes = "ปวดหลัง ส่งตรวจ X-ray",                              CreatedAt = now.AddDays(-15) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000008"), TenantId = TenantId, PatientId = Patient4Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-1),  Notes = "ดูผล X-ray ไม่พบความผิดปกติ แนะนำกายภาพบำบัด",         CreatedAt = now.AddDays(-1) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000009"), TenantId = TenantId, PatientId = Patient5Id, BranchId = Branch2Id, VisitedAt = now.AddDays(-10), Notes = "เบาหวาน ตรวจ HbA1c ระดับ 6.8% ปรับยา",               CreatedAt = now.AddDays(-10) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000010"), TenantId = TenantId, PatientId = Patient6Id, BranchId = Branch1Id, VisitedAt = now.AddDays(-12), Notes = "ไข้หวัด เจ็บคอ สั่งยาปฏิชีวนะ 5 วัน",                 CreatedAt = now.AddDays(-12) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000011"), TenantId = TenantId, PatientId = Patient7Id, BranchId = Branch2Id, VisitedAt = now.AddDays(-8),  Notes = "ตรวจตา สายตาสั้นเพิ่ม ทำแว่นใหม่",                    CreatedAt = now.AddDays(-8) },
            new PatientVisit { Id = Guid.Parse("f0000000-0000-0000-0000-000000000012"), TenantId = TenantId, PatientId = Patient9Id, BranchId = Branch2Id, VisitedAt = now.AddDays(-2),  Notes = "วัคซีนไข้หวัดใหญ่ประจำปี",                           CreatedAt = now.AddDays(-2) },
        };
        db.PatientVisits.AddRange(visits);

        await db.SaveChangesAsync();
    }
}
