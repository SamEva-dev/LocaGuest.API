using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.CreateContract;

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, Result<ContractDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreateContractCommandHandler> _logger;

    public CreateContractCommandHandler(
        ILocaGuestDbContext context,
        ILogger<CreateContractCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Vérifier que la propriété existe
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);
            
            if (property == null)
                return Result.Failure<ContractDto>("Property not found");

            // Vérifier que le locataire existe
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
            
            if (tenant == null)
                return Result.Failure<ContractDto>("Tenant not found");

            // Parser le type de contrat
            if (!Enum.TryParse<ContractType>(request.Type, out var contractType))
                contractType = ContractType.Unfurnished;

            // Créer le contrat
            var contract = Contract.Create(
                request.PropertyId,
                request.TenantId,
                contractType,
                request.StartDate,
                request.EndDate,
                request.Rent,
                request.Deposit
            );

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync(cancellationToken);

            var contractDto = new ContractDto
            {
                Id = contract.Id,
                PropertyId = contract.PropertyId,
                TenantId = contract.TenantId,
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
