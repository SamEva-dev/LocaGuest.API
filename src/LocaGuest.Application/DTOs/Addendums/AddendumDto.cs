namespace LocaGuest.Application.DTOs.Addendums;

using System.Text.Json;

public record AddendumDto(
    Guid Id,
    Guid ContractId,
    string Type,
    DateTime EffectiveDate,
    string Reason,
    string Description,
    decimal? OldRent,
    decimal? NewRent,
    decimal? OldCharges,
    decimal? NewCharges,
    DateTime? OldEndDate,
    DateTime? NewEndDate,
    string? OccupantChanges,
    Guid? OldRoomId,
    Guid? NewRoomId,
    string? OldClauses,
    string? NewClauses,
    List<Guid> AttachedDocumentIds,
    string SignatureStatus,
    DateTime? SignedDate,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class CreateAddendumDto
{
    public Guid ContractId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal? NewRent { get; set; }
    public decimal? NewCharges { get; set; }
    public DateTime? NewEndDate { get; set; }
    public JsonElement? OccupantChanges { get; set; }
    public Guid? NewRoomId { get; set; }
    public string? NewClauses { get; set; }

    public List<Guid>? AttachedDocumentIds { get; set; }
    public string? Notes { get; set; }
    public bool SendEmail { get; set; }
    public bool RequireSignature { get; set; }
}

public class UpdateAddendumDto
{
    public DateTime? EffectiveDate { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }

    public List<Guid>? AttachedDocumentIds { get; set; }
    public string? Notes { get; set; }
}
