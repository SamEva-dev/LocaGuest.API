using System.Security.Cryptography;
using System.Text;
using LocaGuest.Application.Common.Exceptions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Exceptions;
using Serilog;

namespace LocaGuest.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
    {
        try
        {
            if (!IsWriteMethod(context.Request.Method))
            {
                await _next(context);
                return;
            }

            context.Request.EnableBuffering();

            var bodyBytes = await ReadRequestBodyBytesAsync(context);

            var idempotencyKey = GetOrDeriveIdempotencyKey(context, bodyBytes);
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Validation Error",
                    "Idempotency-Key header is required.",
                    "https://tools.ietf.org/html/rfc9110#section-15.5.1");
                return;
            }

            var clientId = GetClientId(context);
            var requestHash = ComputeRequestHash(context.Request.Method, context.Request.Path, context.Request.QueryString, bodyBytes);

            var existing = await store.GetAsync(clientId, idempotencyKey, context.RequestAborted);

            if (existing is not null)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                    throw new IdempotencyConflictException("Idempotency-Key reuse with different payload.");

                if (existing.StatusCode == 0)
                    throw new IdempotencyConflictException("Idempotency-Key request is still in progress.");

                Log.Information(
                    "idempotency.replay correlationId={CorrelationId} clientId={ClientId} idempotencyKey={IdempotencyKey} method={Method} path={Path} statusCode={StatusCode}",
                    context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier,
                    clientId,
                    idempotencyKey,
                    context.Request.Method,
                    context.Request.Path.Value,
                    existing.StatusCode);

                await ReplayAsync(context, existing);
                return;
            }

            var placeholder = await store.CreatePlaceholderAsync(
                clientId,
                idempotencyKey,
                requestHash,
                context.RequestAborted);

            var originalBody = context.Response.Body;
            await using var mem = new MemoryStream();
            context.Response.Body = mem;

            try
            {
                await _next(context);

                mem.Position = 0;
                var responseBytes = mem.ToArray();
                var contentType = context.Response.ContentType ?? "";

                await store.CompleteAsync(
                    placeholder.Id,
                    context.Response.StatusCode,
                    contentType,
                    Convert.ToBase64String(responseBytes),
                    responseJson: string.Empty,
                    context.RequestAborted);

                Log.Information(
                    "idempotency.stored correlationId={CorrelationId} clientId={ClientId} idempotencyKey={IdempotencyKey} method={Method} path={Path} statusCode={StatusCode}",
                    context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier,
                    clientId,
                    idempotencyKey,
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode);

                mem.Position = 0;
                await mem.CopyToAsync(originalBody, context.RequestAborted);
            }
            catch
            {
                try
                {
                    await store.DeleteAsync(placeholder.Id, context.RequestAborted);
                }
                catch
                {
                    // ignore cleanup failures
                }

                throw;
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
        catch (IdempotencyConflictException ex)
        {
            Log.Warning(
                "idempotency.conflict correlationId={CorrelationId} method={Method} path={Path} message={Message}",
                context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path.Value,
                ex.Message);

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status409Conflict,
                "Idempotency conflict",
                ex.Message,
                "https://httpstatuses.com/409");
        }
    }

    private static bool IsWriteMethod(string method)
        => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    private static async Task<byte[]> ReadRequestBodyBytesAsync(HttpContext context)
    {
        context.Request.Body.Position = 0;
        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
        context.Request.Body.Position = 0;
        return ms.ToArray();
    }

    private static string GetClientId(HttpContext context)
    {
        var user = context.User;
        var clientId =
            user.FindFirst("azp")?.Value ??
            user.FindFirst("client_id")?.Value ??
            user.FindFirst("sub")?.Value ??
            user.FindFirst("nameid")?.Value ??
            "anonymous";

        return clientId;
    }

    private static string? GetOrDeriveIdempotencyKey(HttpContext context, byte[] bodyBytes)
    {
        if (context.Request.Path.StartsWithSegments("/api/webhooks/stripe", StringComparison.OrdinalIgnoreCase))
        {
            var stripeSig = context.Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(stripeSig))
                return null;

            var bodyHash = Sha256Hex(bodyBytes);
            return $"stripe:{stripeSig}:{bodyHash}";
        }

        var key = context.Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }

    private static string ComputeRequestHash(string method, PathString path, QueryString query, byte[] bodyBytes)
    {
        var prefix = $"{method} {path}{query}";
        using var sha = SHA256.Create();

        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        sha.TransformBlock(prefixBytes, 0, prefixBytes.Length, null, 0);
        sha.TransformFinalBlock(bodyBytes, 0, bodyBytes.Length);

        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }

    private static string Sha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task ReplayAsync(HttpContext context, IdempotencyRecord existing)
    {
        context.Response.StatusCode = existing.StatusCode;

        if (!string.IsNullOrWhiteSpace(existing.ResponseContentType))
            context.Response.ContentType = existing.ResponseContentType;

        if (!string.IsNullOrWhiteSpace(existing.ResponseBodyBase64))
        {
            var bytes = Convert.FromBase64String(existing.ResponseBodyBase64);
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        }
        else if (!string.IsNullOrWhiteSpace(existing.ResponseJson))
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseJson, context.RequestAborted);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int status,
        string title,
        string detail,
        string type)
    {
        var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier;

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type
        };

        problem.Extensions["correlationId"] = correlationId;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var json = System.Text.Json.JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json, context.RequestAborted);
    }
}
