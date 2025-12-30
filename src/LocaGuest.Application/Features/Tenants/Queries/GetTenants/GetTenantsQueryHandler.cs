using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, Result<PagedResult<TenantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantsQueryHandler> _logger;

    public GetTenantsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResult<TenantDto>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Tenants.Query();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(t =>
                    t.FullName.ToLower().Contains(searchLower) ||
                    t.Email.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<TenantStatus>(request.Status, ignoreCase: true, out var status))
            {
                query = query.Where(t => t.Status == status);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var tenants = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var dtos = tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Code = t.Code,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                Status = t.Status.ToString(),
                ActiveContracts = 0, // TODO: Calculate from contracts
                MoveInDate = t.MoveInDate,
                CreatedAt = t.CreatedAt,
                HasIdentityDocument = false,
                PropertyId = t.PropertyId,
                PropertyCode = t.PropertyCode
            }).ToList();

            var result = new PagedResult<TenantDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return Result.Failure<PagedResult<TenantDto>>($"Error retrieving tenants: {ex.Message}");
        }
    }
}
