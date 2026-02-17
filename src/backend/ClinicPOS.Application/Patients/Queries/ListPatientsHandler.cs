using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Patients.Queries;

public class ListPatientsHandler
{
    private readonly IPatientRepository _patients;

    public ListPatientsHandler(IPatientRepository patients)
    {
        _patients = patients;
    }

    public async Task<List<Patient>> HandleAsync(ListPatientsQuery query, CancellationToken ct = default)
    {
        return await _patients.GetAllAsync(query.BranchId, ct);
    }
}
