using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence.Repositories;

public class PatientVisitRepository : IPatientVisitRepository
{
    private readonly AppDbContext _db;

    public PatientVisitRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PatientVisit> AddAsync(PatientVisit visit, CancellationToken ct = default)
    {
        _db.PatientVisits.Add(visit);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(visit).Reference(v => v.Branch).LoadAsync(ct);
        return visit;
    }

    public async Task<List<PatientVisit>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
    {
        return await _db.PatientVisits
            .Include(v => v.Branch)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitedAt)
            .ToListAsync(ct);
    }
}
