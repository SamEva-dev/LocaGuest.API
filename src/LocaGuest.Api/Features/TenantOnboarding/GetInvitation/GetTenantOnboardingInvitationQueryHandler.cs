using System.Security.Cryptography;
using System.Text;
using LocaGuest.Api.Services;
using LocaGuest.Domain.Entities;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Features.TenantOnboarding.GetInvitation;

public sealed class GetTenantOnboardingInvitationQueryHandler
    : IRequestHandler<GetTenantOnboardingInvitationQuery, GetTenantOnboardingInvitationResult>
{
    private readonly ITenantOnboardingTokenService _tokenService;
    private readonly LocaGuestDbContext _db;

    public GetTenantOnboardingInvitationQueryHandler(
        ITenantOnboardingTokenService tokenService,
        LocaGuestDbContext db)
    {
        _tokenService = tokenService;
        _db = db;
    }

    public async Task<GetTenantOnboardingInvitationResult> Handle(
        GetTenantOnboardingInvitationQuery request,
        CancellationToken ct)
    {
        var token = (request.Token ?? string.Empty).Trim();
        if (!_tokenService.TryValidate(token, out var payload))
        {
            return new GetTenantOnboardingInvitationResult(
                IsValid: false,
                Message: "Lien invalide ou expiré.",
                Invitation: null);
        }

        var tokenHash = ComputeSha256Hex(token);

        // Endpoint public => IgnoreQueryFilters puis scope org.
        var invitation = await _db.TenantOnboardingInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.OrganizationId == payload.OrganizationId && i.TokenHash == tokenHash, ct);

        if (invitation == null)
        {
            return new GetTenantOnboardingInvitationResult(
                IsValid: false,
                Message: "Lien invalide ou expiré.",
                Invitation: null);
        }

        if (invitation.IsUsed())
        {
            return new GetTenantOnboardingInvitationResult(
                IsValid: false,
                Message: "Ce dossier a déjà été transmis.",
                Invitation: null);
        }

        if (invitation.IsExpired(DateTime.UtcNow))
        {
            return new GetTenantOnboardingInvitationResult(
                IsValid: false,
                Message: "Lien expiré. Veuillez demander un nouveau lien à votre propriétaire.",
                Invitation: null);
        }

        return new GetTenantOnboardingInvitationResult(
            IsValid: true,
            Message: null,
            Invitation: new GetTenantOnboardingInvitationResponse(
                Email: invitation.Email,
                PropertyId: invitation.PropertyId,
                ExpiresAtUtc: invitation.ExpiresAtUtc));
    }

    private static string ComputeSha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
