using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command for creating a new tenant
/// </summary>
public record CreateTenantCommand : IRequest<Result<TenantDetailDto>>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Nationality { get; init; }
    public string? IdNumber { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
    public string? Occupation { get; init; }
    public decimal? MonthlyIncome { get; init; }
    public string? Notes { get; init; }
}
