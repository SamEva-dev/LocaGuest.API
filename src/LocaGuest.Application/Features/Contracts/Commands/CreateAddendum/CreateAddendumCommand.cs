using LocaGuest.Application.Common;
using System.Text.Json;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateAddendum;

/// <summary>
/// Commande pour créer un avenant au contrat existant
/// </summary>
public record CreateAddendumCommand : IRequest<Result<Guid>>
{
    public Guid ContractId { get; init; }
    
    /// <summary>
    /// Type d'avenant: Financial, Duration, Occupants, Clauses, Free
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Date d'entrée en vigueur de l'avenant
    /// </summary>
    public DateTime EffectiveDate { get; init; }
    
    /// <summary>
    /// Motif/raison de l'avenant
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// Description détaillée
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    // ========== MODIFICATIONS FINANCIÈRES ==========
    public decimal? NewRent { get; init; }
    public decimal? NewCharges { get; init; }
    
    // ========== MODIFICATIONS DURÉE ==========
    public DateTime? NewEndDate { get; init; }
    
    // ========== MODIFICATIONS OCCUPANTS ==========
    /// <summary>
    /// Modifications d'occupants (DTO structuré) - sera sérialisé en JSON dans l'entité Addendum
    /// </summary>
    public JsonElement? OccupantChanges { get; init; }
    
    // ========== MODIFICATIONS CHAMBRE ==========
    public Guid? NewRoomId { get; init; }
    
    // ========== MODIFICATIONS CLAUSES ==========
    public string? NewClauses { get; init; }
    
    // ========== DOCUMENTS & OPTIONS ==========
    public List<Guid> AttachedDocumentIds { get; init; } = new();
    public string? Notes { get; init; }
    public bool SendEmail { get; init; }
    public bool RequireSignature { get; init; }
}
