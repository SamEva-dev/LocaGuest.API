using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateContract;

public record CreateContractCommand : IRequest<Result<ContractDto>>
{
    public Guid PropertyId { get; init; }
    public Guid OccupantId { get; init; }
    public string Type { get; init; } = "Unfurnished";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal Rent { get; init; }
    public decimal Charges { get; init; } = 0;
    public decimal? Deposit { get; init; }
    public decimal? DepositAmountExpected { get; init; }
    public DateTime? DepositDueDate { get; init; }
    public bool DepositAllowInstallments { get; init; }
    public int PaymentDueDay { get; init; } = 5; // Jour limite de paiement (1-31)
    public Guid? RoomId { get; init; }
    public string? Notes { get; init; }
}
