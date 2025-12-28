using System.Text.Json;
using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Addendums;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Addendums.Queries.GetAddendums;

public class GetAddendumsQueryHandler : IRequestHandler<GetAddendumsQuery, Result<PagedResult<AddendumDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAddendumsQueryHandler> _logger;

    public GetAddendumsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAddendumsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AddendumDto>>> Handle(GetAddendumsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 ? 50 : request.PageSize;

            var query = _unitOfWork.Addendums.Query().AsNoTracking();

            if (request.ContractId.HasValue)
                query = query.Where(x => x.ContractId == request.ContractId);

            if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<AddendumType>(request.Type, true, out var typeEnum))
                query = query.Where(x => x.Type == typeEnum);

            if (!string.IsNullOrWhiteSpace(request.SignatureStatus)
                && Enum.TryParse<AddendumSignatureStatus>(request.SignatureStatus, true, out var sigEnum))
                query = query.Where(x => x.SignatureStatus == sigEnum);

            if (request.FromUtc.HasValue)
                query = query.Where(x => x.EffectiveDate >= request.FromUtc.Value);

            if (request.ToUtc.HasValue)
                query = query.Where(x => x.EffectiveDate <= request.ToUtc.Value);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.EffectiveDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<AddendumDto>
            {
                Items = items.Select(Map).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addendums");
            return Result.Failure<PagedResult<AddendumDto>>($"Error getting addendums: {ex.Message}");
        }
    }

    private static AddendumDto Map(Addendum entity)
    {
        var docs = new List<Guid>();
        if (!string.IsNullOrWhiteSpace(entity.AttachedDocumentIds))
        {
            try
            {
                docs = JsonSerializer.Deserialize<List<Guid>>(entity.AttachedDocumentIds) ?? new List<Guid>();
            }
            catch
            {
                docs = new List<Guid>();
            }
        }

        return new AddendumDto(
            entity.Id,
            entity.ContractId,
            entity.Type.ToString(),
            entity.EffectiveDate,
            entity.Reason,
            entity.Description,
            entity.OldRent,
            entity.NewRent,
            entity.OldCharges,
            entity.NewCharges,
            entity.OldEndDate,
            entity.NewEndDate,
            entity.OccupantChanges,
            entity.OldRoomId,
            entity.NewRoomId,
            entity.OldClauses,
            entity.NewClauses,
            docs,
            entity.SignatureStatus.ToString(),
            entity.SignedDate,
            entity.Notes,
            entity.CreatedAt,
            entity.LastModifiedAt);
    }
}
