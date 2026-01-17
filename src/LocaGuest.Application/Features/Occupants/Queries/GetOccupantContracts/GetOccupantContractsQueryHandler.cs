using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupantContracts;

public class GetOccupantContractsQueryHandler : IRequestHandler<GetOccupantContractsQuery, Result<List<ContractListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOccupantContractsQueryHandler> _logger;

    public GetOccupantContractsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOccupantContractsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<ContractListDto>>> Handle(GetOccupantContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var occupant = await _unitOfWork.Occupants.GetByIdAsync(request.OccupantId, cancellationToken);
            if (occupant == null)
                return Result.Failure<List<ContractListDto>>($"Occupant with ID {request.OccupantId} not found");

            var contracts = await _unitOfWork.Contracts.Query()
                .Where(c => c.RenterOccupantId == request.OccupantId)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync(cancellationToken);

            var contractDtos = contracts.Select(c => new ContractListDto
            {
                Id = c.Id,
                PropertyId = c.PropertyId,
                PropertyName = string.Empty,
                OccupantId = c.RenterOccupantId,
                OccupantName = string.Empty,
                Type = c.Type.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.Rent,
                Deposit = c.Deposit ?? 0,
                Status = c.Status.ToString()
            }).ToList();

            _logger.LogInformation("Retrieved {Count} contracts for occupant {OccupantId}", contractDtos.Count, request.OccupantId);
            return Result.Success(contractDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts for occupant {OccupantId}", request.OccupantId);
            return Result.Failure<List<ContractListDto>>("Error retrieving occupant contracts");
        }
    }
}
