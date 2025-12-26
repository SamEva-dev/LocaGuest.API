using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.UpdateContract;

public record UpdateContractCommand : IRequest<Result>
{
    public required Guid ContractId { get; init; }

    public Guid? TenantId { get; init; }
    public bool TenantIdIsSet { get; init; }

    public Guid? PropertyId { get; init; }
    public bool PropertyIdIsSet { get; init; }

    public Guid? RoomId { get; init; }
    public bool RoomIdIsSet { get; init; }

    public string? Type { get; init; }
    public bool TypeIsSet { get; init; }

    public DateTime? StartDate { get; init; }
    public bool StartDateIsSet { get; init; }

    public DateTime? EndDate { get; init; }
    public bool EndDateIsSet { get; init; }

    public decimal? Rent { get; init; }
    public bool RentIsSet { get; init; }

    public decimal? Charges { get; init; }
    public bool ChargesIsSet { get; init; }

    public decimal? Deposit { get; init; }
    public bool DepositIsSet { get; init; }
}
