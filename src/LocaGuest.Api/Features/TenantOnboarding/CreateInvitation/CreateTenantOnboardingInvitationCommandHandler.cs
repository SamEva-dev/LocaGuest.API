using LocaGuest.Api.Services;
using LocaGuest.Domain.Entities;
using LocaGuest.Emailing.Abstractions;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Api.Features.TenantOnboarding.CreateInvitation;

public sealed class CreateTenantOnboardingInvitationCommandHandler
    : IRequestHandler<CreateTenantOnboardingInvitationCommand, CreateTenantOnboardingInvitationResponse>
{
    private readonly ITenantOnboardingTokenService _tokenService;
    private readonly IEmailingService _emailing;
    private readonly IConfiguration _configuration;
    private readonly LocaGuestDbContext _db;

    public CreateTenantOnboardingInvitationCommandHandler(
        ITenantOnboardingTokenService tokenService,
        IEmailingService emailing,
        IConfiguration configuration,
        LocaGuestDbContext db)
    {
        _tokenService = tokenService;
        _emailing = emailing;
        _configuration = configuration;
        _db = db;
    }

    public async Task<CreateTenantOnboardingInvitationResponse> Handle(
        CreateTenantOnboardingInvitationCommand request,
        CancellationToken ct)
    {
        var orgId = request.OrganizationId;
        if (orgId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.");

        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        var ttlHours = _configuration.GetValue<int?>("TenantOnboarding:InvitationTtlHours") ?? 168;

        var payload = new TenantOnboardingTokenPayload(
            OrganizationId: orgId,
            Email: email,
            PropertyId: request.PropertyId,
            ExpiresAtUtc: DateTime.UtcNow);

        var token = _tokenService.CreateToken(payload, TimeSpan.FromHours(ttlHours));
        var expiresAtUtc = DateTime.UtcNow.AddHours(ttlHours);

        var frontendUrl = (_configuration["App:FrontendUrl"] ?? string.Empty).TrimEnd('/');
        var link = $"{frontendUrl}/public/tenant-onboarding?token={Uri.EscapeDataString(token)}";

        var invitation = TenantOnboardingInvitation.Create(
            organizationId: orgId,
            email: email,
            propertyId: request.PropertyId,
            expiresAtUtc: expiresAtUtc,
            token: token);

        _db.TenantOnboardingInvitations.Add(invitation);

        var subject = "Complétez votre dossier locataire - LocaGuest";
        var body = $@"
<h2>Bonjour,</h2>
<p>Vous avez été invité(e) à compléter votre <strong>dossier locataire</strong>.</p>
<p>Pour remplir le formulaire et déposer vos documents, cliquez sur ce lien :</p>
<p><a href=""{link}"" target=""_blank"" rel=""noreferrer"">Compléter mon dossier</a></p>
<p>Ce lien est valable jusqu'au <strong>{expiresAtUtc:dd/MM/yyyy HH:mm}</strong> (UTC).</p>
<br/>
<p>Cordialement,<br/>L'équipe LocaGuest</p>";

        await _emailing.QueueHtmlAsync(
            toEmail: email,
            subject: subject,
            htmlContent: body,
            textContent: null,
            attachments: null,
            tags: EmailUseCaseTags.AccessInviteUser,
            cancellationToken: ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // TokenHash unique: collision extrêmement improbable, mais on renvoie un message user-friendly.
            throw new InvalidOperationException("Impossible de créer l'invitation.");
        }

        return new CreateTenantOnboardingInvitationResponse(token, link, expiresAtUtc);
    }
}
