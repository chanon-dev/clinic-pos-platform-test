using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Appointments.Queries;

public class ListAppointmentsHandler
{
    private readonly IAppointmentRepository _appointments;

    public ListAppointmentsHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<List<Appointment>> HandleAsync(ListAppointmentsQuery query, CancellationToken ct = default)
    {
        return await _appointments.GetAllAsync(query.BranchId, query.Date, ct);
    }
}
