using System.Security.Cryptography;
using System.Text;
using LocaGuest.Application.Services;
using LocaGuest.Api.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Features.TenantOnboarding.Submit;

public sealed class SubmitTenantOnboardingCommandHandler
    : IRequestHandler<SubmitTenantOnboardingCommand, SubmitTenantOnboardingResult>
{
    private readonly ITenantOnboardingTokenService _tokenService;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LocaGuestDbContext _db;

    public SubmitTenantOnboardingCommandHandler(
        ITenantOnboardingTokenService tokenService,
        INumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork,
        LocaGuestDbContext db)
    {
        _tokenService = tokenService;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
        _db = db;
    }

    public async Task<SubmitTenantOnboardingResult> Handle(SubmitTenantOnboardingCommand request, CancellationToken ct)
    {
        if (!_tokenService.TryValidate(request.Token, out var payload))
            return new SubmitTenantOnboardingResult(false, "Lien invalide ou expiré.", null);

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return new SubmitTenantOnboardingResult(false, "Le prénom et le nom sont obligatoires.", null);

        var orgId = payload.OrganizationId;
        var email = payload.Email;
        var utcNow = DateTime.UtcNow;

        var tokenHash = ComputeSha256Hex(request.Token);
        var invitation = await _db.TenantOnboardingInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.TokenHash == tokenHash, ct);

        if (invitation == null)
            return new SubmitTenantOnboardingResult(false, "Lien invalide ou expiré.", null);

        if (invitation.IsUsed())
            return new SubmitTenantOnboardingResult(false, "Ce dossier a déjà été transmis.", null);

        if (invitation.IsExpired(utcNow))
            return new SubmitTenantOnboardingResult(false, "Lien expiré. Veuillez demander un nouveau lien à votre propriétaire.", null);

        // Règle demandée : si l'occupant existe déjà, on stoppe.
        var existing = await _db.Occupants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrganizationId == orgId && o.Email == email, ct);

        if (existing != null)
            return new SubmitTenantOnboardingResult(false, "Un locataire avec cet email existe déjà. Veuillez demander un nouveau lien à votre propriétaire.", null);

        var fullName = $"{request.FirstName} {request.LastName}".Trim();

        var dateOfBirth = ToUtc(request.DateOfBirth);
        var identityExpiryDate = ToUtc(request.IdentityExpiryDate);

        var code = await _numberSequenceService.GenerateNextCodeAsync(orgId, EntityPrefixes.Occupant, ct);
        var occupant = Occupant.Create(fullName, email, request.Phone);
        occupant.SetOrganizationId(orgId);
        occupant.SetCode(code);

        occupant.UpdateDetails(
            dateOfBirth: dateOfBirth,
            address: request.Address,
            city: request.City,
            postalCode: request.PostalCode,
            country: request.Country,
            nationality: request.Nationality,
            idNumber: request.IdNumber,
            emergencyContact: request.EmergencyContact,
            emergencyPhone: request.EmergencyPhone,
            occupation: request.Occupation,
            monthlyIncome: request.MonthlyIncome,
            notes: request.Notes);

        if (payload.PropertyId.HasValue)
        {
            var property = await _db.Properties
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Id == payload.PropertyId.Value, ct);
            if (property != null)
            {
                occupant.AssociateToProperty(property.Id, property.Code);
            }
        }

        await _unitOfWork.Occupants.AddAsync(occupant, ct);

        try
        {
            await _unitOfWork.CommitAsync(ct);
        }
        catch (InvalidOperationException)
        {
            return new SubmitTenantOnboardingResult(false, "Impossible de valider votre dossier pour le moment. Veuillez demander un nouveau lien à votre propriétaire.", null);
        }

        if (request.IdentityDocument != null)
        {
            if (!identityExpiryDate.HasValue)
                return new SubmitTenantOnboardingResult(false, "La date de fin de validité est obligatoire si vous joignez une pièce d'identité.", null);

            await SaveDocumentAsync(orgId, request.IdentityDocument, DocumentType.PieceIdentite, DocumentCategory.Identite, occupant.Id, occupant.PropertyId, "Pièce d'identité", identityExpiryDate, ct);
        }

        if (request.AddressProof != null)
            await SaveDocumentAsync(orgId, request.AddressProof, DocumentType.JustificatifDomicile, DocumentCategory.Justificatifs, occupant.Id, occupant.PropertyId, "Justificatif de domicile", null, ct);

        if (request.IncomeProofs != null)
        {
            foreach (var f in request.IncomeProofs.Where(x => x != null))
                await SaveDocumentAsync(orgId, f, DocumentType.BulletinSalaire, DocumentCategory.Justificatifs, occupant.Id, occupant.PropertyId, "Bulletin de salaire", null, ct);
        }

        if (request.GuarantyProof != null)
            await SaveDocumentAsync(orgId, request.GuarantyProof, DocumentType.Autre, DocumentCategory.Justificatifs, occupant.Id, occupant.PropertyId, "Garantie", null, ct);

        if (request.GuarantorIdentity != null)
            await SaveDocumentAsync(orgId, request.GuarantorIdentity, DocumentType.PieceIdentite, DocumentCategory.Justificatifs, occupant.Id, occupant.PropertyId, "Pièce d'identité du garant", null, ct);

        if (request.GuarantorIncomeProofs != null)
        {
            foreach (var f in request.GuarantorIncomeProofs.Where(x => x != null))
                await SaveDocumentAsync(orgId, f, DocumentType.BulletinSalaire, DocumentCategory.Justificatifs, occupant.Id, occupant.PropertyId, "Justificatif de revenus du garant", null, ct);
        }

        invitation.MarkAsUsed(utcNow, occupant.Id);
        await _db.SaveChangesAsync(ct);

        return new SubmitTenantOnboardingResult(true, null, occupant.Id);
    }

    private async Task SaveDocumentAsync(
        Guid organizationId,
        IFormFile file,
        DocumentType type,
        DocumentCategory category,
        Guid occupantId,
        Guid? propertyId,
        string? description,
        DateTime? expiryDate,
        CancellationToken ct)
    {
        if (file.Length == 0) return;

        expiryDate = ToUtc(expiryDate);

        var originalBaseName = Path.GetFileNameWithoutExtension(file.FileName);
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{SanitizeFileName(originalBaseName)}_{Guid.NewGuid():N}{extension}";

        var documentsPath = Path.Combine(AppContext.BaseDirectory, "Documents", organizationId.ToString());
        Directory.CreateDirectory(documentsPath);
        var filePath = Path.Combine(documentsPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var code = await _numberSequenceService.GenerateNextCodeAsync(organizationId, EntityPrefixes.Document, ct);

        var doc = Document.Create(
            fileName: fileName,
            filePath: filePath,
            type: type,
            category: category,
            fileSizeBytes: file.Length,
            tenantId: occupantId,
            propertyId: propertyId,
            description: description,
            expiryDate: expiryDate);

        doc.SetOrganizationId(organizationId);
        doc.SetCode(code);

        await _unitOfWork.Documents.AddAsync(doc, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (!value.HasValue) return null;

        var dt = value.Value;
        if (dt.Kind == DateTimeKind.Utc) return dt;

        if (dt.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        return dt.ToUniversalTime();
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            return $"file_{Guid.NewGuid():N}";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(safeName.Length);
        foreach (var ch in safeName)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return sb.ToString().TrimEnd('.');
    }

    private static string ComputeSha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
