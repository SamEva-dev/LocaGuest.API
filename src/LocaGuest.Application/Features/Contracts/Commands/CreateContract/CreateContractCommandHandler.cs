using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateContract;

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, Result<ContractDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly ILogger<CreateContractCommandHandler> _logger;

    public CreateContractCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        INumberSequenceService numberSequenceService,
        ILogger<CreateContractCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _numberSequenceService = numberSequenceService;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant authentication
            if (!_tenantContext.IsAuthenticated)
                return Result.Failure<ContractDto>("User not authenticated");

            // Vérifier que la propriété existe
            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            
            if (property == null)
                return Result.Failure<ContractDto>("Property not found");

            // Vérifier que le locataire existe
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
            
            if (tenant == null)
                return Result.Failure<ContractDto>("Tenant not found");

            // ⭐ VALIDATION MÉTIER 1: Vérifier que le locataire est disponible
            if (!tenant.IsAvailableForNewContract())
            {
                _logger.LogWarning(
                    "Tenant {TenantCode} is not available (Status: {Status}). Cannot create new contract.",
                    tenant.Code,
                    tenant.Status);
                return Result.Failure<ContractDto>(
                    $"Le locataire {tenant.FullName} n'est pas disponible. " +
                    $"Statut actuel: {tenant.Status}. Un locataire ne peut avoir qu'un seul contrat actif ou signé.");
            }
            
            // ⭐ Vérifier que le locataire n'est pas déjà associé à un autre bien
            if (tenant.PropertyId.HasValue && tenant.PropertyId.Value != request.PropertyId)
            {
                _logger.LogWarning(
                    "Tenant {TenantCode} is already associated to property {PropertyId}",
                    tenant.Code,
                    tenant.PropertyId.Value);
                return Result.Failure<ContractDto>(
                    $"Le locataire {tenant.FullName} est déjà associé à un autre bien. Veuillez le dissocier d'abord.");
            }
            
            // ⭐ VALIDATION MÉTIER 2: Vérifier que le bien est disponible
            if (!property.IsAvailableForNewContract())
            {
                _logger.LogWarning(
                    "Property {PropertyCode} is not available (Status: {Status}, Usage: {UsageType}). Cannot create new contract.",
                    property.Code,
                    property.Status,
                    property.UsageType);
                return Result.Failure<ContractDto>(
                    $"Le bien {property.Name} n'est pas disponible pour un nouveau contrat. " +
                    $"Statut: {property.Status}.");
            }
            
            // ⭐ VALIDATION MÉTIER 3: Pour colocation individuelle, vérifier RoomId obligatoire
            if (property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.ColocationIndividual || 
                property.UsageType == Domain.Aggregates.PropertyAggregate.PropertyUsageType.Colocation)
            {
                if (!request.RoomId.HasValue)
                {
                    return Result.Failure<ContractDto>(
                        "Pour une colocation individuelle, l'identifiant de la chambre (RoomId) est obligatoire.");
                }
                
                // TODO: Vérifier que la chambre est disponible (nécessite une entité Room)
                // Pour l'instant, on suppose que la validation est faite côté UI
            }

            // Parser le type de contrat
            if (!Enum.TryParse<ContractType>(request.Type, out var contractType))
                contractType = ContractType.Unfurnished;

            // ✅ QUICK WIN: Generate automatic code
            var code = await _numberSequenceService.GenerateNextCodeAsync(
                _tenantContext.TenantId!.Value,
                EntityPrefixes.Contract,
                cancellationToken);

            _logger.LogInformation("Generated code for new contract: {Code}", code);

            // Créer le contrat
            var contract = Contract.Create(
                request.PropertyId,
                request.TenantId,
                contractType,
                request.StartDate,
                request.EndDate,
                request.Rent,
                request.Charges,
                request.Deposit,
                request.PaymentDueDay,
                request.RoomId
            );

            // ✅ Set the generated code
            contract.SetCode(code);

            // ⭐ Associate tenant to property (bidirectional)
            tenant.AssociateToProperty(property.Id, property.Code);
            property.AddTenant(tenant.Code);

            // ⭐ Reactivate tenant (tenant becomes active when associated to a property)
            tenant.Reactivate();

            _logger.LogInformation(
                "Associated tenant {TenantCode} to property {PropertyCode} and reactivated tenant",
                tenant.Code,
                property.Code);

            await _unitOfWork.Contracts.AddAsync(contract, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var contractDto = new ContractDto
            {
                Id = contract.Id,
                Code = contract.Code,  // ✅ Include generated code
                PropertyId = contract.PropertyId,
                TenantId = contract.RenterTenantId,
                PropertyName = property.Name,
                TenantName = tenant.FullName,
                Type = contract.Type.ToString(),
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Rent = contract.Rent,
                Deposit = contract.Deposit,
                Status = contract.Status.ToString(),
                Notes = request.Notes,
                PaymentsCount = 0,
                CreatedAt = contract.CreatedAt
            };

            _logger.LogInformation("Contract created: {ContractId} for Property {PropertyId} and Tenant {TenantId}", 
                contract.Id, request.PropertyId, request.TenantId);

            return Result.Success(contractDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract for Property {PropertyId} and Tenant {TenantId}", 
                request.PropertyId, request.TenantId);
            return Result.Failure<ContractDto>($"Error creating contract: {ex.Message}");
        }
    }
}
