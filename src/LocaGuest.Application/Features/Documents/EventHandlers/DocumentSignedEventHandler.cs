using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate.Events;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.EventHandlers;

/// <summary>
/// Handler pour l'événement DocumentSigned
/// Met à jour le contrat associé et vérifie si tous les documents requis sont signés
/// </summary>
public class DocumentSignedEventHandler : INotificationHandler<DocumentSigned>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentSignedEventHandler> _logger;

    public DocumentSignedEventHandler(
        IUnitOfWork unitOfWork,
        ILogger<DocumentSignedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DocumentSigned notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Document signed: {DocumentId}, Type: {Type}, ContractId: {ContractId}",
            notification.DocumentId,
            notification.Type,
            notification.ContractId);

        // Si le document n'est pas associé à un contrat, on ne fait rien
        if (notification.ContractId == null)
        {
            _logger.LogInformation("Document {DocumentId} not associated with a contract, skipping contract update", 
                notification.DocumentId);
            return;
        }

        // Récupérer le contrat
        var contract = await _unitOfWork.Contracts.GetByIdAsync(notification.ContractId.Value, cancellationToken);
        if (contract == null)
        {
            _logger.LogWarning("Contract {ContractId} not found for document {DocumentId}", 
                notification.ContractId, notification.DocumentId);
            return;
        }

        var previousStatus = contract.Status;

        // Notifier le contrat qu'un document a été signé
        contract.OnDocumentSigned(notification.Type);

        var newStatus = contract.Status;

        // Sauvegarder les changements
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Contract {ContractId} updated after document signature. Status: {PreviousStatus} → {NewStatus}",
            contract.Id, previousStatus, newStatus);

        // Si le contrat est maintenant Signed, marquer la propriété comme Reserved
        // Si Active, marquer comme Occupied
        if (newStatus == ContractStatus.Signed)
        {
            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
            if (property != null)
            {
                property.SetStatus(PropertyStatus.Reserved);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Property {PropertyId} marked as Reserved due to contract {ContractId} being Signed",
                    property.Id, contract.Id);
            }
        }
        else if (newStatus == ContractStatus.Active)
        {
            var property = await _unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
            if (property != null)
            {
                property.SetStatus(PropertyStatus.Occupied);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Property {PropertyId} marked as Occupied due to contract {ContractId} being Active",
                    property.Id, contract.Id);
            }
        }
    }
}
