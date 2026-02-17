using ClinicPOS.Application.Common.Interfaces;
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

    public async Task<List<Patient>> HandleAsync(ListPatientsQuery query, CancellationToken ct = default)
    {
        var cacheKey = $"patients:{_tenantContext.TenantId}:{query.BranchId?.ToString() ?? "all"}";

        var cached = await _cache.GetAsync<List<Patient>>(cacheKey, ct);
        if (cached != null) return cached;

        var patients = await _patients.GetAllAsync(query.BranchId, ct);
        await _cache.SetAsync(cacheKey, patients, TimeSpan.FromMinutes(5), ct);
        return patients;
    }
}
