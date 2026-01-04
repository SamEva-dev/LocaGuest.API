using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LocaGuest.Api.Common.Swagger;

public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        if (string.IsNullOrWhiteSpace(method))
            return;

        if (!IsWriteMethod(method))
            return;

        operation.Parameters ??= new List<OpenApiParameter>();

        if (operation.Parameters.All(p => !string.Equals(p.Name, "Idempotency-Key", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Required for all write requests (POST/PUT/PATCH/DELETE). Used to safely retry without duplicating side effects.",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }

        AddProblemDetailsResponse(operation, "400", "Bad Request");
        AddProblemDetailsResponse(operation, "409", "Conflict");
        AddProblemDetailsResponse(operation, "429", "Too Many Requests");
        AddProblemDetailsResponse(operation, "500", "Internal Server Error");
    }

    private static bool IsWriteMethod(string method)
        => string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
           || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase)
           || string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase)
           || string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase);

    private static void AddProblemDetailsResponse(OpenApiOperation operation, string statusCode, string description)
    {
        operation.Responses ??= new OpenApiResponses();

        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses[statusCode] = new OpenApiResponse { Description = description };
        }

        operation.Responses[statusCode].Content ??= new Dictionary<string, OpenApiMediaType>();

        if (!operation.Responses[statusCode].Content.ContainsKey("application/problem+json"))
        {
            operation.Responses[statusCode].Content["application/problem+json"] = new OpenApiMediaType();
        }
    }
}
