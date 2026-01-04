using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocaGuest.Application.Common.Exceptions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Entities;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Services;

public sealed class InvitationProvisioningService : IInvitationProvisioningService
{
    private readonly LocaGuestDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public InvitationProvisioningService(LocaGuestDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<ConsumeInvitationResponse> ConsumeInvitationAsync(
        ConsumeInvitationRequest request,
        string idempotencyKey,
        CancellationToken ct)
    {
        var user = _http.HttpContext?.User ?? throw new InvalidOperationException("No HttpContext.");

        var clientId =
            user.FindFirstValue("azp") ??
            user.FindFirstValue("client_id") ??
            user.FindFirstValue("sub") ??
            "unknown-client";

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        var requestHash = Sha256Hex(requestJson);

        var existing = await _db.IdempotencyRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IdempotencyKey == idempotencyKey, ct);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                throw new IdempotencyConflictException("Idempotency-Key reuse with different payload.");

            var cached = JsonSerializer.Deserialize<ConsumeInvitationResponse>(existing.ResponseJson, _jsonOptions);
            if (cached is null)
                throw new InvalidOperationException("Invalid cached idempotency response.");

            return cached;
        }

        if (!Guid.TryParse(request.UserId, out var parsedUserId) || parsedUserId == Guid.Empty)
            throw new InvalidOperationException("UserId must be a valid GUID.");

        var (invitationId, secret) = ParseToken(request.Token);

        var invitation = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, ct);

        if (invitation == null)
            throw new InvalidOperationException("Invitation not found.");

        var now = DateTime.UtcNow;

        if (!invitation.IsValidNow(now))
        {
            invitation.MarkExpired();
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Invitation is not valid.");
        }

        if (!invitation.VerifySecret(secret))
            throw new InvalidOperationException("Invalid invitation token.");

        if (!string.Equals(invitation.Email, (request.UserEmail ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invitation email mismatch.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var role = string.IsNullOrWhiteSpace(invitation.Role) ? TeamRoles.Occupant : invitation.Role;

        var member = await _db.TeamMembers
            .FirstOrDefaultAsync(tm => tm.UserId == parsedUserId && tm.OrganizationId == invitation.OrganizationId, ct);

        if (member == null)
        {
            member = new TeamMember(
                userId: parsedUserId,
                organizationId: invitation.OrganizationId,
                role: role,
                userEmail: request.UserEmail,
                invitedBy: invitation.CreatedByUserId);

            member.SetOrganizationId(invitation.OrganizationId);

            _db.TeamMembers.Add(member);
        }
        else
        {
            member.UpdateRole(role);
            member.AcceptInvitation();
        }

        invitation.Accept(now);

        await _db.SaveChangesAsync(ct);

        var response = new ConsumeInvitationResponse(
            OrganizationId: invitation.OrganizationId,
            TeamMemberId: member.Id,
            Role: role);

        var entity = new IdempotencyRequestEntity
        {
            ClientId = clientId,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            StatusCode = 200,
            ResponseJson = JsonSerializer.Serialize(response, _jsonOptions),
            CompletedAtUtc = DateTime.UtcNow
        };

        _db.IdempotencyRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return response;
    }

    private static (Guid InvitationId, string Secret) ParseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Token is required.");

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("Invalid token format.");

        if (!Guid.TryParse(parts[0], out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Invalid token format.");

        return (id, parts[1]);
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
