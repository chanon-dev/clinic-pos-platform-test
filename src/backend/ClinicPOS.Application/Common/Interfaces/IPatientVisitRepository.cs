using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Common.Interfaces;

public interface IPatientVisitRepository
{
    Task<PatientVisit> AddAsync(PatientVisit visit, CancellationToken ct = default);
    Task<List<PatientVisit>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
}
