using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Common.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment> AddAsync(Appointment appointment, CancellationToken ct = default);
    Task<List<Appointment>> GetAllAsync(Guid? branchId = null, DateOnly? date = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid patientId, Guid branchId, DateTime startAt, CancellationToken ct = default);
}
