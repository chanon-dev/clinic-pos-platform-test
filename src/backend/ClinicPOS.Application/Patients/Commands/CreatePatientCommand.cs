namespace ClinicPOS.Application.Patients.Commands;

public record CreatePatientCommand(
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid? PrimaryBranchId);
