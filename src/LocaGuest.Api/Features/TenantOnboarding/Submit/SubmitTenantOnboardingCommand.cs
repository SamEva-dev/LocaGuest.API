using LocaGuest.Domain.Aggregates.DocumentAggregate;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LocaGuest.Api.Features.TenantOnboarding.Submit;

public sealed record SubmitTenantOnboardingCommand(
    string Token,
    string FirstName,
    string LastName,
    string? Phone,
    DateTime? DateOfBirth,
    string? Address,
    string? City,
    string? PostalCode,
    string? Country,
    string? Nationality,
    string? IdNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Occupation,
    decimal? MonthlyIncome,
    string? Notes,
    DateTime? IdentityExpiryDate,
    IFormFile? IdentityDocument,
    IFormFile? AddressProof,
    IFormFile? GuarantyProof,
    IFormFile? GuarantorIdentity,
    List<IFormFile>? IncomeProofs,
    List<IFormFile>? GuarantorIncomeProofs) : IRequest<SubmitTenantOnboardingResult>;

public sealed record SubmitTenantOnboardingResult(
    bool IsSuccess,
    string? Message,
    Guid? OccupantId);
