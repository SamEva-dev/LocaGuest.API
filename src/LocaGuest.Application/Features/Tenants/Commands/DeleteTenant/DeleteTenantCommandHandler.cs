using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result<DeleteTenantResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    public DeleteTenantCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteTenantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DeleteTenantResult>> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _unitOfWork.Occupants.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
                return Result.Failure<DeleteTenantResult>($"Tenant with ID {request.TenantId} not found");

            // ✅ VALIDATION: Vérifier qu'il n'y a pas de contrats actifs ou signés
            var activeContracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.RenterTenantId == request.TenantId &&
                           (c.Status == ContractStatus.Active ||
                            c.Status == ContractStatus.Signed))
                .ToListAsync(cancellationToken);

            if (activeContracts.Any())
            {
                return Result.Failure<DeleteTenantResult>(
                    $"Impossible de supprimer le locataire. Il possède {activeContracts.Count} contrat(s) actif(s) ou signé(s). " +
                    "Veuillez d'abord résilier ces contrats.");
            }

            // ✅ CASCADE: Récupérer tous les contrats (Draft, Cancelled, Expired, Terminated)
            var allContracts = await _unitOfWork.Contracts.Query()
                .Include(c => c.Payments)
                .Where(c => c.RenterTenantId == request.TenantId)
                .ToListAsync(cancellationToken);

            int deletedPayments = 0;
            int deletedDocuments = 0;

            // Supprimer tous les paiements
            foreach (var contract in allContracts)
            {
                if (contract.Payments.Any())
                {
                    deletedPayments += contract.Payments.Count;
                }
            }

            // Supprimer les documents des contrats
            var contractIds = allContracts.Select(c => c.Id).ToList();
            var contractDocuments = await _unitOfWork.Documents.Query()
                .Where(d => contractIds.Contains(d.ContractId!.Value))
                .ToListAsync(cancellationToken);

            foreach (var doc in contractDocuments)
            {
                _unitOfWork.Documents.Remove(doc);
                deletedDocuments++;
            }

            // Supprimer les documents directement liés au locataire
            var tenantDocuments = await _unitOfWork.Documents.Query()
                .Where(d => d.AssociatedTenantId == request.TenantId)
                .ToListAsync(cancellationToken);

            foreach (var doc in tenantDocuments)
            {
                _unitOfWork.Documents.Remove(doc);
                deletedDocuments++;
            }

            // Supprimer les contrats
            foreach (var contract in allContracts)
            {
                _unitOfWork.Contracts.Remove(contract);
            }

            // Supprimer le locataire
            _unitOfWork.Occupants.Remove(tenant);

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Locataire {TenantId} (Code: {TenantCode}) supprimé avec succès. " +
                "Contrats: {ContractCount}, Paiements: {PaymentCount}, Documents: {DocumentCount}",
                request.TenantId, tenant.Code, allContracts.Count, deletedPayments, deletedDocuments);

            var result = new DeleteTenantResult
            {
                Id = tenant.Id,
                DeletedContracts = allContracts.Count,
                DeletedPayments = deletedPayments,
                DeletedDocuments = deletedDocuments
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de la suppression du locataire {TenantId}", request.TenantId);
            return Result.Failure<DeleteTenantResult>("Erreur lors de la suppression du locataire");
        }
    }
}
