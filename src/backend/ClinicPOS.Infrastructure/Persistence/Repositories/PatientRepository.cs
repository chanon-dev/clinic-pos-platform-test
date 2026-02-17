using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _db;

    public PatientRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Patient> AddAsync(Patient patient, CancellationToken ct = default)
    {
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);
        // Load the branch navigation property
        if (patient.PrimaryBranchId.HasValue)
        {
            await _db.Entry(patient).Reference(p => p.PrimaryBranch).LoadAsync(ct);
        }
        return patient;
    }

    public async Task<List<Patient>> GetAllAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var query = _db.Patients
            .Include(p => p.PrimaryBranch)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(p => p.PrimaryBranchId == branchId.Value);

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task<bool> PhoneExistsAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await _db.Patients.AnyAsync(p => p.PhoneNumber == phoneNumber, ct);
    }
}
