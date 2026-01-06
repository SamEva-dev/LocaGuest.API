using LocaGuest.Domain.Common;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.PropertyAggregate;

/// <summary>
/// Représente une chambre dans une colocation
/// Entity qui fait partie de l'agrégat Property
/// </summary>
public class PropertyRoom : Entity
{
    public Guid PropertyId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal? Surface { get; private set; }
    public decimal Rent { get; private set; }
    public decimal? Charges { get; private set; }
    public string? Description { get; private set; }
    public PropertyRoomStatus Status { get; private set; }

    public DateTime? OnHoldUntilUtc { get; private set; }
    
    /// <summary>
    /// ID du contrat actuellement lié à cette chambre (si Signed/Active)
    /// Null si la chambre est libre
    /// </summary>
    public Guid? CurrentContractId { get; private set; }

    private PropertyRoom() { } // EF Core

    public static PropertyRoom Create(
        Guid propertyId,
        Guid organizationId,
        string name,
        decimal rent,
        decimal? surface = null,
        decimal? charges = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("ROOM_NAME_REQUIRED", "Room name is required");
            
        if (rent <= 0)
            throw new ValidationException("ROOM_INVALID_RENT", "Room rent must be positive");
            
        if (surface.HasValue && surface.Value <= 0)
            throw new ValidationException("ROOM_INVALID_SURFACE", "Room surface must be positive");

        var room = new PropertyRoom
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Name = name,
            Surface = surface,
            Rent = rent,
            Charges = charges,
            Description = description,
            Status = PropertyRoomStatus.Available
        };

        room.SetOrganizationId(organizationId);
        return room;
    }

    public void SetOrganizationId(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty.", nameof(organizationId));

        if (OrganizationId != Guid.Empty && OrganizationId != organizationId)
            throw new InvalidOperationException("OrganizationId is immutable once set.");

        OrganizationId = organizationId;
    }

    /// <summary>
    /// Mettre la chambre en pré-réservation (OnHold) pendant une durée limitée
    /// </summary>
    public void Hold(Guid contractId, DateTime holdUntilUtc)
    {
        if (Status == PropertyRoomStatus.Occupied)
            throw new ValidationException("ROOM_ALREADY_OCCUPIED", "Room is already occupied");

        if (Status == PropertyRoomStatus.Reserved && CurrentContractId.HasValue && CurrentContractId != contractId)
            throw new ValidationException("ROOM_ALREADY_RESERVED", "Room is already reserved");

        if (Status == PropertyRoomStatus.OnHold && CurrentContractId.HasValue && CurrentContractId != contractId)
            throw new ValidationException("ROOM_ALREADY_ON_HOLD", "Room is already on hold");

        Status = PropertyRoomStatus.OnHold;
        CurrentContractId = contractId;
        OnHoldUntilUtc = holdUntilUtc.Kind == DateTimeKind.Utc
            ? holdUntilUtc
            : DateTime.SpecifyKind(holdUntilUtc, DateTimeKind.Utc);
    }

    /// <summary>
    /// Réserver la chambre pour un contrat
    /// </summary>
    public void Reserve(Guid contractId)
    {
        if (Status == PropertyRoomStatus.Occupied)
            throw new ValidationException("ROOM_ALREADY_OCCUPIED", "Room is already occupied");
            
        if (Status == PropertyRoomStatus.Reserved && CurrentContractId.HasValue && CurrentContractId != contractId)
            throw new ValidationException("ROOM_ALREADY_RESERVED", "Room is already reserved");

        if (Status == PropertyRoomStatus.OnHold && CurrentContractId.HasValue && CurrentContractId != contractId)
            throw new ValidationException("ROOM_ALREADY_ON_HOLD", "Room is already on hold");

        Status = PropertyRoomStatus.Reserved;
        CurrentContractId = contractId;
        OnHoldUntilUtc = null;
    }

    /// <summary>
    /// Marquer la chambre comme occupée (contrat actif)
    /// </summary>
    public void Occupy(Guid contractId)
    {
        if (Status == PropertyRoomStatus.Occupied && CurrentContractId != contractId)
            throw new ValidationException("ROOM_ALREADY_OCCUPIED_BY_ANOTHER", "Room is already occupied by another contract");

        if ((Status == PropertyRoomStatus.Reserved || Status == PropertyRoomStatus.OnHold) &&
            CurrentContractId.HasValue &&
            CurrentContractId != contractId)
            throw new ValidationException("ROOM_ALREADY_OCCUPIED_BY_ANOTHER", "Room is already occupied by another contract");

        Status = PropertyRoomStatus.Occupied;
        CurrentContractId = contractId;
        OnHoldUntilUtc = null;
    }

    /// <summary>
    /// Libérer la chambre (fin de contrat)
    /// </summary>
    public void Release()
    {
        Status = PropertyRoomStatus.Available;
        CurrentContractId = null;
        OnHoldUntilUtc = null;
    }

    /// <summary>
    /// Vérifier si la chambre est disponible pour un nouveau contrat
    /// </summary>
    public bool IsAvailable()
    {
        return Status == PropertyRoomStatus.Available;
    }

    /// <summary>
    /// Mettre à jour les détails de la chambre
    /// </summary>
    public void UpdateDetails(
        string? name = null,
        decimal? rent = null,
        decimal? surface = null,
        decimal? charges = null,
        string? description = null)
    {
        if (name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("ROOM_NAME_REQUIRED", "Room name is required");
            Name = name;
        }
        
        if (rent.HasValue)
        {
            if (rent.Value <= 0)
                throw new ValidationException("ROOM_INVALID_RENT", "Room rent must be positive");
            Rent = rent.Value;
        }
        
        if (surface.HasValue)
        {
            if (surface.Value <= 0)
                throw new ValidationException("ROOM_INVALID_SURFACE", "Room surface must be positive");
            Surface = surface.Value;
        }
        
        if (charges.HasValue) Charges = charges.Value;
        if (description != null) Description = description;
    }
}

/// <summary>
/// Statut d'une chambre de colocation
/// </summary>
public enum PropertyRoomStatus
{
    /// <summary>
    /// Chambre disponible, aucun contrat
    /// </summary>
    Available = 0,

    /// <summary>
    /// Chambre réservée (contrat signé, début futur)
    /// </summary>
    Reserved = 1,
    
    /// <summary>
    /// Chambre occupée (contrat actif)
    /// </summary>
    Occupied = 2,

    OnHold = 3
}
