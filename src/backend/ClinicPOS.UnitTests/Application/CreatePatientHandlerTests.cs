using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Patients.Commands;
using ClinicPOS.Domain.Entities;
using NSubstitute;

namespace ClinicPOS.UnitTests.Application;

public class CreatePatientHandlerTests
{
    private readonly IPatientRepository _patientRepo;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cache;
    private readonly CreatePatientHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreatePatientHandlerTests()
    {
        _patientRepo = Substitute.For<IPatientRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = Substitute.For<ICacheService>();
        _tenantContext.TenantId.Returns(_tenantId);
        _handler = new CreatePatientHandler(_patientRepo, _tenantContext, _cache);
    }

    [Fact]
    public async Task Should_Create_Patient_Successfully()
    {
        _patientRepo.PhoneExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _patientRepo.AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Patient>());

        var result = await _handler.HandleAsync(
            new CreatePatientCommand("John", "Doe", "0812345678", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value!.FirstName);
        Assert.Equal(_tenantId, result.Value.TenantId);
    }

    [Fact]
    public async Task Should_Fail_When_Phone_Duplicate()
    {
        _patientRepo.PhoneExistsAsync("0812345678", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.HandleAsync(
            new CreatePatientCommand("John", "Doe", "0812345678", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("already exists", result.Error!);
    }

    [Fact]
    public async Task Should_Fail_When_FirstName_Empty()
    {
        var result = await _handler.HandleAsync(
            new CreatePatientCommand("", "Doe", "0812345678", null));

        Assert.False(result.IsSuccess);
        Assert.Contains("First name", result.Error!);
    }

    [Fact]
    public async Task Should_Fail_When_PhoneNumber_Empty()
    {
        var result = await _handler.HandleAsync(
            new CreatePatientCommand("John", "Doe", "", null));

        Assert.False(result.IsSuccess);
        Assert.Contains("Phone number", result.Error!);
    }

    [Fact]
    public async Task Should_Set_TenantId_From_Context()
    {
        _patientRepo.PhoneExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _patientRepo.AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Patient>());

        var result = await _handler.HandleAsync(
            new CreatePatientCommand("Jane", "Smith", "0898765432", null));

        Assert.True(result.IsSuccess);
        Assert.Equal(_tenantId, result.Value!.TenantId);
        await _patientRepo.Received(1).AddAsync(
            Arg.Is<Patient>(p => p.TenantId == _tenantId),
            Arg.Any<CancellationToken>());
    }
}
