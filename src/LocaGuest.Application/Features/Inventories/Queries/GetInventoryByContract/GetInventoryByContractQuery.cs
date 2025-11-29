using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Queries.GetInventoryByContract;

/// <summary>
/// Récupère les EDL (entrée + sortie) pour un contrat
/// </summary>
public record GetInventoryByContractQuery(Guid ContractId) : IRequest<Result<ContractInventoriesDto>>;

public class ContractInventoriesDto
{
    public InventoryEntryDto? Entry { get; set; }
    public InventoryExitDto? Exit { get; set; }
    public bool HasEntry => Entry != null;
    public bool HasExit => Exit != null;
}
