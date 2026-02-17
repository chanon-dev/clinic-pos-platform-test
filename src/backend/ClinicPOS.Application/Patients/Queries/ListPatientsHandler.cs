using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Patients.Queries;

public class ListPatientsHandler
{
    private readonly IPatientRepository _patients;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public ListPatientsHandler(IPatientRepository patients, ICacheService cache, ITenantContext tenantContext)
    {
        _patients = patients;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<Patient>> HandleAsync(ListPatientsQuery query, CancellationToken ct = default)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);
        var cacheKey = $"patients:{_tenantContext.TenantId}:{query.BranchId?.ToString() ?? "all"}:{query.Cursor ?? "first"}:{limit}:{query.Search ?? ""}";

        var cached = await _cache.GetAsync<PagedResult<Patient>>(cacheKey, ct);
        if (cached != null) return cached;

        var result = await _patients.GetPagedAsync(query.Cursor, limit, query.BranchId, query.Search, ct);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}
