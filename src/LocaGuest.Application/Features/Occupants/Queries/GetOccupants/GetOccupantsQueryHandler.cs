using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Occupants;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupants;

public class GetOccupantsQueryHandler : IRequestHandler<GetOccupantsQuery, Result<PagedResult<OccupantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOccupantsQueryHandler> _logger;

    public GetOccupantsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOccupantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResult<OccupantDto>>> Handle(GetOccupantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Occupants.Query(asNoTracking: true);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(t =>
                    t.FullName.ToLower().Contains(searchLower) ||
                    t.Email.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<OccupantStatus>(request.Status, ignoreCase: true, out var status))
            {
                query = query.Where(t => t.Status == status);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var occupants = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var dtos = occupants.Select(t => new OccupantDto
            {
                Id = t.Id,
                Code = t.Code,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                Status = t.Status.ToString(),
                ActiveContracts = 0,
                MoveInDate = t.MoveInDate,
                CreatedAt = t.CreatedAt,
                HasIdentityDocument = false,
                PropertyId = t.PropertyId,
                PropertyCode = t.PropertyCode
            }).ToList();

            var result = new PagedResult<OccupantDto>
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
            _logger.LogError(ex, "Error retrieving occupants");
            return Result.Failure<PagedResult<OccupantDto>>($"Error retrieving occupants: {ex.Message}");
        }
    }
}
