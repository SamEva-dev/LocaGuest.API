using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Documents.Commands.GenerateQuittance;

public record GenerateQuittanceCommand : IRequest<Result<byte[]>>
{
    public required string TenantId { get; init; }
    public required string PropertyId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime PaymentDate { get; init; }
    public required string Month { get; init; } // "Janvier 2025"
    public string? Reference { get; init; }
}
