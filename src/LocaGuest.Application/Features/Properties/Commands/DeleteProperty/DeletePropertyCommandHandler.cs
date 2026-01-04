using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.DeleteProperty;

public class DeletePropertyCommandHandler : IRequestHandler<DeletePropertyCommand, Result<DeletePropertyResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<DeletePropertyCommandHandler> _logger;

    public DeletePropertyCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<DeletePropertyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result<DeletePropertyResult>> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_orgContext.IsAuthenticated)
            {
                return Result.Failure<DeletePropertyResult>("User not authenticated");
            }

            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<DeletePropertyResult>("Property not found");

            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.PropertyId == request.PropertyId &&
                            (c.Status == ContractStatus.Active || c.Status == ContractStatus.Signed))
                .ToListAsync(cancellationToken);

            if (activeContracts.Any())
            {
                return Result.Failure<DeletePropertyResult>(
                    $"Impossible de supprimer le bien. Il possède {activeContracts.Count} contrat(s) actif(s) ou signé(s). Veuillez d'abord résilier ces contrats.");
            }

            var allContracts = await _unitOfWork.Contracts.Query()
                .Include(c => c.Payments)
                .Where(c => c.PropertyId == request.PropertyId)
                .ToListAsync(cancellationToken);

            int deletedPayments = 0;
            int deletedDocuments = 0;

            foreach (var contract in allContracts)
            {
                // TODO: Migrate to new PaymentAggregate system
                // Old ContractPayment system is deprecated
                // if (contract.Payments.Any())
                // {
                //     deletedPayments += contract.Payments.Count;
                // }

                var contractDocuments = await _unitOfWork.Documents.Query()
                    .Where(d => d.ContractId == contract.Id)
                    .ToListAsync(cancellationToken);

                foreach (var doc in contractDocuments)
                {
                    _unitOfWork.Documents.Remove(doc);
                    deletedDocuments++;
                }
            }

            foreach (var contract in allContracts)
            {
                _unitOfWork.Contracts.Remove(contract);
            }

            var propertyDocuments = await _unitOfWork.Documents.Query()
                .Where(d => d.PropertyId == request.PropertyId)
                .ToListAsync(cancellationToken);

            foreach (var doc in propertyDocuments)
            {
                _unitOfWork.Documents.Remove(doc);
                deletedDocuments++;
            }

            var associatedTenants = await _unitOfWork.Occupants.Query()
                .Where(t => t.PropertyId == request.PropertyId)
                .ToListAsync(cancellationToken);

            foreach (var tenant in associatedTenants)
            {
                tenant.DissociateFromProperty();
            }

            _unitOfWork.Properties.Remove(property);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Bien {PropertyId} (Code: {PropertyCode}) supprimé avec succès. Contrats: {ContractCount}, Paiements: {PaymentCount}, Documents: {DocumentCount}, Locataires dissociés: {TenantCount}",
                property.Id,
                property.Code,
                allContracts.Count,
                deletedPayments,
                deletedDocuments,
                associatedTenants.Count);

            return Result.Success(new DeletePropertyResult
            {
                Id = property.Id,
                DeletedContracts = allContracts.Count,
                DeletedPayments = deletedPayments,
                DeletedDocuments = deletedDocuments,
                DissociatedTenants = associatedTenants.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de la suppression du bien {PropertyId}", request.PropertyId);
            return Result.Failure<DeletePropertyResult>("Erreur lors de la suppression du bien");
        }
    }
}
