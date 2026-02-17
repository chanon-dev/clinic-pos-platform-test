using System.Text;
using System.Text.Json;
using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
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

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Patients
            .Include(p => p.PrimaryBranch)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(string? cursor, int limit, Guid? branchId = null, string? search = null, CancellationToken ct = default)
    {
        var query = _db.Patients
            .Include(p => p.PrimaryBranch)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(p => p.PrimaryBranchId == branchId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.FirstName, term) ||
                EF.Functions.ILike(p.LastName, term) ||
                EF.Functions.ILike(p.PhoneNumber, term));
        }

        // Decode cursor (CreatedAt|Id)
        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            if (cursorDate.HasValue && cursorId.HasValue)
            {
                query = query.Where(p =>
                    p.CreatedAt < cursorDate.Value ||
                    (p.CreatedAt == cursorDate.Value && p.Id.CompareTo(cursorId.Value) < 0));
            }
        }

        var totalCount = await _db.Patients
            .Where(p => !branchId.HasValue || p.PrimaryBranchId == branchId.Value)
            .CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore) items = items.Take(limit).ToList();

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items[^1];
            nextCursor = EncodeCursor(last.CreatedAt, last.Id);
        }

        return PagedResult<Patient>.Create(items, nextCursor, hasMore, totalCount);
    }

    public async Task<bool> PhoneExistsAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await _db.Patients.AnyAsync(p => p.PhoneNumber == phoneNumber, ct);
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var json = JsonSerializer.Serialize(new { c = createdAt.ToString("O"), i = id });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static (DateTime? Date, Guid? Id) DecodeCursor(string cursor)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var obj = JsonSerializer.Deserialize<JsonElement>(json);
            var date = DateTime.Parse(obj.GetProperty("c").GetString()!);
            var id = Guid.Parse(obj.GetProperty("i").GetString()!);
            return (date, id);
        }
        catch
        {
            return (null, null);
        }
    }
}
