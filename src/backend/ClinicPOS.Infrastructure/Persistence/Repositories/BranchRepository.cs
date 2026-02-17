using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Infrastructure.Persistence.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly AppDbContext _db;

    public BranchRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Branch>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Branches.OrderBy(b => b.Name).ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid branchId, CancellationToken ct = default)
    {
        return await _db.Branches.AnyAsync(b => b.Id == branchId, ct);
    }
}
