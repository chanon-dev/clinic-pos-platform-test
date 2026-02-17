using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.PatientVisits.Commands;

public class RecordVisitHandler
{
    private readonly IPatientVisitRepository _visits;
    private readonly IPatientRepository _patients;
    private readonly ITenantContext _tenantContext;

    public RecordVisitHandler(IPatientVisitRepository visits, IPatientRepository patients, ITenantContext tenantContext)
    {
        _visits = visits;
        _patients = patients;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PatientVisit>> HandleAsync(RecordVisitCommand command, CancellationToken ct = default)
    {
        var patient = await _patients.GetByIdAsync(command.PatientId, ct);
        if (patient == null)
            return Result<PatientVisit>.Failure("Patient not found.", 404);

        var visit = new PatientVisit
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            PatientId = command.PatientId,
            BranchId = command.BranchId,
            VisitedAt = command.VisitedAt,
            Notes = command.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _visits.AddAsync(visit, ct);
        return Result<PatientVisit>.Success(visit);
    }
}
