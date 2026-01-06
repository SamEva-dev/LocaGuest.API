using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate.Events;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.EventHandlers;

/// <summary>
/// Handler pour l'événement DocumentSigned
/// Met à jour le contrat associé et vérifie si tous les documents requis sont signés
/// </summary>
public class DocumentSignedEventHandler : INotificationHandler<DocumentSigned>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<DocumentSignedEventHandler> _logger;

    public DocumentSignedEventHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestReadDbContext readDb,
        ILogger<DocumentSignedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _readDb = readDb;
        _logger = logger;
    }

    public async Task Handle(DocumentSigned notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Document signed: {DocumentId}, Type: {Type}",
            notification.DocumentId,
            notification.Type);

        var contractIds = await _readDb.ContractDocumentLinks
            .AsNoTracking()
            .Where(x => x.DocumentId == notification.DocumentId)
            .Select(x => x.ContractId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (contractIds.Count == 0)
        {
            _logger.LogInformation("Document {DocumentId} not linked to any contract, skipping contract update", notification.DocumentId);
            return;
        }

        foreach (var contractId in contractIds)
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId, cancellationToken);
            if (contract == null)
            {
                _logger.LogWarning("Contract {ContractId} not found for document {DocumentId}", contractId, notification.DocumentId);
                continue;
            }

            var previousStatus = contract.Status;
            contract.OnDocumentSigned(notification.Type);
            var newStatus = contract.Status;

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Contract {ContractId} updated after document signature. Status: {PreviousStatus} → {NewStatus}",
                contract.Id, previousStatus, newStatus);

            if (newStatus == ContractStatus.Signed)
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
                if (property != null)
                {
                    property.SetStatus(PropertyStatus.Reserved);
                    await _unitOfWork.CommitAsync(cancellationToken);
                }
            }
            else if (newStatus == ContractStatus.Active)
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
                if (property != null)
                {
                    property.SetStatus(PropertyStatus.Active);
                    property.SetStatus(PropertyStatus.Occupied);
                    await _unitOfWork.CommitAsync(cancellationToken);
                }
            }
        }
    }
}
