using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Entities;

/// <summary>
/// Représente un membre d'une équipe (lien User <-> Organization avec rôle)
/// </summary>
public class TeamMember : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Role { get; private set; } = string.Empty; // Admin, Manager, Viewer, etc.
    public string UserEmail { get; private set; } = string.Empty; // Stocké pour éviter dépendance auth
    public Guid? InvitedBy { get; private set; }
    public DateTime InvitedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? RemovedAt { get; private set; }

    // Navigation property
    public Organization? Organization { get; set; }

    private TeamMember() { } // EF Core

    public TeamMember(
        Guid userId,
        Guid organizationId,
        string role,
        string userEmail,
        Guid? invitedBy = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        OrganizationId = organizationId;
        Role = role ?? TeamRoles.Viewer;
        UserEmail = userEmail;
        InvitedBy = invitedBy;
        InvitedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void AcceptInvitation()
    {
        AcceptedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void UpdateRole(string newRole)
    {
        if (string.IsNullOrWhiteSpace(newRole))
            throw new ArgumentException("Role cannot be empty", nameof(newRole));

        Role = newRole;
    }

    public void Remove()
    {
        IsActive = false;
        RemovedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        RemovedAt = null;
    }
}

/// <summary>
/// Rôles prédéfinis pour les membres d'équipe
/// </summary>
public static class TeamRoles
{
    public const string Owner = "Owner";           // Créateur du tenant, tous les droits
    public const string Admin = "Admin";           // Admin complet
    public const string Manager = "Manager";       // Gestion des biens et locataires
    public const string Accountant = "Accountant"; // Accès finances uniquement
    public const string Viewer = "Viewer";         // Lecture seule

    public const string Occupant = "Occupant";     // Occupant/Locataire - accès minimal

    public static readonly string[] All = { Owner, Admin, Manager, Accountant, Viewer, Occupant };

    public static bool IsValid(string role)
    {
        return All.Contains(role);
    }
}
