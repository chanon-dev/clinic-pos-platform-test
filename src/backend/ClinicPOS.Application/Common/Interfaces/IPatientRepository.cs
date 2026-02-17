using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Common.Interfaces;

public interface IPatientRepository
{
    Task<Patient> AddAsync(Patient patient, CancellationToken ct = default);
    Task<List<Patient>> GetAllAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<bool> PhoneExistsAsync(string phoneNumber, CancellationToken ct = default);
}
