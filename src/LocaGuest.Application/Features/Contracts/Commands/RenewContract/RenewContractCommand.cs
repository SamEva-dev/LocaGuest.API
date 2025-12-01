using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.RenewContract;

/// <summary>
/// Commande pour renouveler un contrat existant
/// Crée un nouveau contrat pré-rempli et clôture l'ancien en status Renewed
/// </summary>
public record RenewContractCommand : IRequest<Result<Guid>>
{
    /// <summary>
    /// ID du contrat à renouveler
    /// </summary>
    public Guid ContractId { get; init; }
    
    // ========== ÉTAPE 1: Informations générales ==========
    
    /// <summary>
    /// Nouvelle date de début (normalement = lendemain de fin de l'ancien)
    /// </summary>
    public DateTime NewStartDate { get; init; }
    
    /// <summary>
    /// Nouvelle date de fin (calculée selon la durée)
    /// </summary>
    public DateTime NewEndDate { get; init; }
    
    /// <summary>
    /// Type de bail (hérité de l'ancien mais peut changer)
    /// </summary>
    public string ContractType { get; init; } = string.Empty;
    
    // ========== ÉTAPE 2: Révision de loyer ==========
    
    /// <summary>
    /// Nouveau loyer (peut être révisé selon IRL)
    /// </summary>
    public decimal NewRent { get; init; }
    
    /// <summary>
    /// Nouvelles charges
    /// </summary>
    public decimal NewCharges { get; init; }
    
    /// <summary>
    /// Ancien IRL pour calcul de révision
    /// </summary>
    public decimal? PreviousIRL { get; init; }
    
    /// <summary>
    /// Nouvel IRL pour calcul de révision
    /// </summary>
    public decimal? CurrentIRL { get; init; }
    
    /// <summary>
    /// Dépôt de garantie (normalement inchangé sauf exception)
    /// </summary>
    public decimal? Deposit { get; init; }
    
    // ========== ÉTAPE 3: Clauses & Documents ==========
    
    /// <summary>
    /// Clauses personnalisées (nouvelles ou modifiées)
    /// </summary>
    public string? CustomClauses { get; init; }
    
    /// <summary>
    /// Notes additionnelles
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Reconduction tacite activée
    /// </summary>
    public bool TacitRenewal { get; init; }
    
    /// <summary>
    /// IDs des documents joints (DPE mis à jour, règlement, etc.)
    /// </summary>
    public List<Guid> AttachedDocumentIds { get; init; } = new();
}
