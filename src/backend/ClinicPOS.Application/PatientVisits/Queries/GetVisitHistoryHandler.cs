using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.PatientVisits.Queries;

public class GetVisitHistoryHandler
{
    private readonly IPatientVisitRepository _visits;

    public GetVisitHistoryHandler(IPatientVisitRepository visits)
    {
        _visits = visits;
    }

    public async Task<List<PatientVisit>> HandleAsync(GetVisitHistoryQuery query, CancellationToken ct = default)
    {
        return await _visits.GetByPatientIdAsync(query.PatientId, ct);
    }
}
