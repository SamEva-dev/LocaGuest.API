using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Commands.DeleteOccupant;

public class DeleteOccupantCommandHandler : IRequestHandler<DeleteOccupantCommand, Result<DeleteOccupantResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<DeleteOccupantCommandHandler> _logger;

    public DeleteOccupantCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ILogger<DeleteOccupantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<DeleteOccupantResult>> Handle(DeleteOccupantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var occupant = await _unitOfWork.Occupants.GetByIdAsync(request.OccupantId, cancellationToken);
            if (occupant == null)
                return Result.Failure<DeleteOccupantResult>($"Occupant with ID {request.OccupantId} not found");

            // ✅ VALIDATION: Vérifier qu'il n'y a pas de contrats actifs ou signés
            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.RenterOccupantId == request.OccupantId &&
                           (c.Status == ContractStatus.Active ||
                            c.Status == ContractStatus.Signed))
                .ToListAsync(cancellationToken);

            if (activeContracts.Any())
            {
                return Result.Failure<DeleteOccupantResult>(
                    $"Impossible de supprimer l'occupant. Il possède {activeContracts.Count} contrat(s) actif(s) ou signé(s). " +
                    "Veuillez d'abord résilier ces contrats.");
            }

            // ✅ CASCADE: Récupérer tous les contrats (Draft, Cancelled, Expired, Terminated)
            var allContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.RenterOccupantId == request.OccupantId)
                .ToListAsync(cancellationToken);

            int deletedPayments = 0;
            int deletedDocuments = 0;

            // Supprimer tous les paiements
            var contractIds = allContracts.Select(c => c.Id).ToList();
            var paymentsToDelete = await _unitOfWork.Payments.Query()
                .Where(p => contractIds.Contains(p.ContractId))
                .ToListAsync(cancellationToken);

            deletedPayments = paymentsToDelete.Count;
            foreach (var payment in paymentsToDelete)
            {
                _unitOfWork.Payments.Remove(payment);
            }

            // Supprimer les documents des contrats
            var contractDocumentIds = await _context.ContractDocumentLinks
                .AsNoTracking()
                .Where(link => contractIds.Contains(link.ContractId))
                .Select(link => link.DocumentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var contractDocuments = await _unitOfWork.Documents.Query()
                .Where(d => contractDocumentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            var contractLinks = await _context.ContractDocumentLinks
                .Where(link => contractIds.Contains(link.ContractId))
                .ToListAsync(cancellationToken);

            foreach (var link in contractLinks)
            {
                _context.ContractDocumentLinks.Remove(link);
            }

            foreach (var doc in contractDocuments)
            {
                _unitOfWork.Documents.Remove(doc);
                deletedDocuments++;
            }

            // Supprimer les documents directement liés à l'occupant
            var occupantDocuments = await _unitOfWork.Documents.Query()
                .Where(d => d.AssociatedOccupantId == request.OccupantId)
                .ToListAsync(cancellationToken);

            foreach (var doc in occupantDocuments)
            {
                _unitOfWork.Documents.Remove(doc);
                deletedDocuments++;
            }

            // Supprimer les contrats
            foreach (var contract in allContracts)
            {
                _unitOfWork.Contracts.Remove(contract);
            }

            // Supprimer l'occupant
            _unitOfWork.Occupants.Remove(occupant);

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Occupant {OccupantId} (Code: {OccupantCode}) supprimé avec succès. " +
                "Contrats: {ContractCount}, Paiements: {PaymentCount}, Documents: {DocumentCount}",
                request.OccupantId, occupant.Code, allContracts.Count, deletedPayments, deletedDocuments);

            var result = new DeleteOccupantResult
            {
                Id = occupant.Id,
                DeletedContracts = allContracts.Count,
                DeletedPayments = deletedPayments,
                DeletedDocuments = deletedDocuments
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de la suppression de l'occupant {OccupantId}", request.OccupantId);
            return Result.Failure<DeleteOccupantResult>("Erreur lors de la suppression de l'occupant");
        }
    }
}
