using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Patients.Commands;

public class CreatePatientHandler
{
    private readonly IPatientRepository _patients;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;

    public CreatePatientHandler(IPatientRepository patients, ITenantContext tenantContext, ICacheService cache)
    {
        _patients = patients;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<Result<Patient>> HandleAsync(CreatePatientCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName))
            return Result<Patient>.Failure("First name is required.");
        if (string.IsNullOrWhiteSpace(command.LastName))
            return Result<Patient>.Failure("Last name is required.");
        if (string.IsNullOrWhiteSpace(command.PhoneNumber))
            return Result<Patient>.Failure("Phone number is required.");

        if (await _patients.PhoneExistsAsync(command.PhoneNumber, ct))
            return Result<Patient>.Failure(
                $"A patient with phone number '{command.PhoneNumber}' already exists in this clinic.", 409);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            PhoneNumber = command.PhoneNumber.Trim(),
            PrimaryBranchId = command.PrimaryBranchId,
            CreatedAt = DateTime.UtcNow
        };

        await _patients.AddAsync(patient, ct);
        await _cache.RemoveByPrefixAsync($"patients:{_tenantContext.TenantId}:", ct);
        return Result<Patient>.Success(patient);
    }
}
