using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Common.Interfaces;

public interface IBranchRepository
{
    Task<List<Branch>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid branchId, CancellationToken ct = default);
}
