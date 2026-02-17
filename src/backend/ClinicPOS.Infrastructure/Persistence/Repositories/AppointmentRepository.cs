using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _db;

    public AppointmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Appointment> AddAsync(Appointment appointment, CancellationToken ct = default)
    {
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(appointment).Reference(a => a.Patient).LoadAsync(ct);
        await _db.Entry(appointment).Reference(a => a.Branch).LoadAsync(ct);
        return appointment;
    }

    public async Task<List<Appointment>> GetAllAsync(Guid? branchId = null, DateOnly? date = null, CancellationToken ct = default)
    {
        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Branch)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(a => a.BranchId == branchId.Value);

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartAt >= start && a.StartAt < end);
        }

        return await query.OrderBy(a => a.StartAt).ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid patientId, Guid branchId, DateTime startAt, CancellationToken ct = default)
    {
        return await _db.Appointments.AnyAsync(
            a => a.PatientId == patientId && a.BranchId == branchId && a.StartAt == startAt, ct);
    }
}
