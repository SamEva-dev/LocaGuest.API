using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Invoices.Commands.MarkInvoiceAsPaid;

public class MarkInvoiceAsPaidCommandHandler : IRequestHandler<MarkInvoiceAsPaidCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkInvoiceAsPaidCommandHandler> _logger;

    public MarkInvoiceAsPaidCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkInvoiceAsPaidCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(MarkInvoiceAsPaidCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _unitOfWork.RentInvoices.GetByIdAsync(request.InvoiceId, cancellationToken);
            
            if (invoice == null)
            {
                return Result<bool>.Failure<bool>("Facture introuvable");
            }

            invoice.MarkAsPaid(request.PaidDate, request.Notes);

            _unitOfWork.RentInvoices.Update(invoice);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Invoice {InvoiceId} marked as paid", request.InvoiceId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice {InvoiceId} as paid", request.InvoiceId);
            return Result<bool>.Failure<bool>($"Erreur lors du marquage de la facture comme payée: {ex.Message}");
        }
    }
}
