using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Invoices.Queries.GetInvoicesByTenant;

public class GetInvoicesByTenantQueryHandler : IRequestHandler<GetInvoicesByTenantQuery, Result<List<InvoiceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetInvoicesByTenantQueryHandler> _logger;

    public GetInvoicesByTenantQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetInvoicesByTenantQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<InvoiceDto>>> Handle(GetInvoicesByTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var invoices = await _unitOfWork.RentInvoices.GetByTenantIdAsync(request.OccupantId, cancellationToken);

            var invoiceDtos = invoices.Select(i => new InvoiceDto(
                i.Id,
                i.ContractId,
                i.RenterOccupantId,
                i.PropertyId,
                i.Month,
                i.Year,
                i.Amount,
                i.DueDate,
                i.PaidDate,
                i.Status.ToString(),
                i.GeneratedAt,
                i.Notes
            )).ToList();

            return Result<List<InvoiceDto>>.Success(invoiceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for tenant {OccupantId}", request.OccupantId);
            return Result<List<InvoiceDto>>.Failure<List<InvoiceDto>>("Erreur lors de la récupération des factures");
        }
    }
}
