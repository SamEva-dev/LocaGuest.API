using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocaGuest.Application.Common.Exceptions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Entities;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Services;

public sealed class ProvisioningService : IProvisioningService
{
    private readonly LocaGuestDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ProvisioningService(LocaGuestDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<ProvisionOrganizationResponse> ProvisionOrganizationAsync(
        ProvisionOrganizationRequest request,
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

            var cached = JsonSerializer.Deserialize<ProvisionOrganizationResponse>(existing.ResponseJson, _jsonOptions);
            if (cached is null)
                throw new InvalidOperationException("Invalid cached idempotency response.");

            return cached;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var number = await GetNextOrganizationNumberAsync(ct);

        var org = Organization.Create(
            number: number,
            name: request.OrganizationName,
            email: request.OrganizationEmail,
            phone: request.OrganizationPhone);

        _db.Organizations.Add(org);

        if (!Guid.TryParse(request.OwnerUserId, out var ownerUserId))
            throw new InvalidOperationException("OwnerUserId must be a valid GUID.");

        var ownerMember = new TeamMember(
            userId: ownerUserId,
            organizationId: org.Id,
            role: TeamRoles.Owner,
            userEmail: request.OwnerEmail,
            invitedBy: null);

        ownerMember.SetOrganizationId(org.Id);

        _db.TeamMembers.Add(ownerMember);

        await _db.SaveChangesAsync(ct);

        var response = new ProvisionOrganizationResponse(
            OrganizationId: org.Id,
            Number: org.Number,
            Code: org.Code,
            Name: org.Name,
            Email: org.Email);

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

    private async Task<int> GetNextOrganizationNumberAsync(CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT nextval('org.organization_number_seq')";
        var scalar = await cmd.ExecuteScalarAsync(ct);

        return Convert.ToInt32(scalar);
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
