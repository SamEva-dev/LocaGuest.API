using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupantDashboard;

public sealed class GetOccupantDashboardQueryHandler : IRequestHandler<GetOccupantDashboardQuery, Result<OccupantDashboardDto>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<GetOccupantDashboardQueryHandler> _logger;

    public GetOccupantDashboardQueryHandler(
        ILocaGuestReadDbContext readDb,
        ILogger<GetOccupantDashboardQueryHandler> logger)
    {
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<OccupantDashboardDto>> Handle(GetOccupantDashboardQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var occupant = await _readDb.Occupants.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == request.OccupantId, cancellationToken);

            if (occupant == null)
                return Result.Failure<OccupantDashboardDto>($"Occupant with ID {request.OccupantId} not found");

            var contracts = await _readDb.Contracts.AsNoTracking()
                .Where(c => c.RenterOccupantId == request.OccupantId)
                .OrderByDescending(c => c.StartDate)
                .Select(c => new OccupantDashboardContractDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Type = c.Type.ToString(),
                    Status = c.Status.ToString(),
                    PropertyId = c.PropertyId,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Rent = c.Rent,
                    Deposit = c.Deposit.Value,
                    Charges = c.Charges
                })
                .ToListAsync(cancellationToken);

            var payments = await _readDb.Payments.AsNoTracking()
                .Where(p => p.RenterOccupantId == request.OccupantId)
                .ToListAsync(cancellationToken);

            var totalPaid = payments.Sum(p => p.AmountPaid);
            var totalPayments = payments.Count;
            var latePayments = payments.Count(p => p.Status == PaymentStatus.Late || p.Status == PaymentStatus.PaidLate);
            var onTimeRate = totalPayments > 0 ? (decimal)(totalPayments - latePayments) / totalPayments : 1.0m;

            var documentsQuery = _readDb.Documents.AsNoTracking()
                .Where(d => d.AssociatedOccupantId == request.OccupantId);

            var totalDocs = await documentsQuery.CountAsync(cancellationToken);
            var activeDocs = await documentsQuery.Where(d => !d.IsArchived).CountAsync(cancellationToken);

            var recentDocs = await documentsQuery
                .Where(d => !d.IsArchived)
                .OrderByDescending(d => d.CreatedAt)
                .Take(10)
                .Select(d => new OccupantDashboardDocumentDto
                {
                    Id = d.Id,
                    Type = d.Type.ToString(),
                    FileName = d.FileName,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var dto = new OccupantDashboardDto
            {
                Occupant = new OccupantDashboardOccupantDto
                {
                    Id = occupant.Id,
                    Code = occupant.Code,
                    FullName = occupant.FullName,
                    Email = occupant.Email,
                    Phone = occupant.Phone,
                    Status = occupant.Status.ToString(),
                    PropertyId = occupant.PropertyId,
                    PropertyCode = occupant.PropertyCode
                },
                Contracts = contracts,
                PaymentStats = new OccupantDashboardPaymentStatsDto
                {
                    TotalPaid = totalPaid,
                    TotalPayments = totalPayments,
                    LatePayments = latePayments,
                    OnTimeRate = onTimeRate
                },
                Documents = new OccupantDashboardDocumentsDto
                {
                    TotalCount = totalDocs,
                    ActiveCount = activeDocs,
                    Recent = recentDocs
                }
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving occupant dashboard for {OccupantId}", request.OccupantId);
            return Result.Failure<OccupantDashboardDto>("Error retrieving occupant dashboard");
        }
    }
}
