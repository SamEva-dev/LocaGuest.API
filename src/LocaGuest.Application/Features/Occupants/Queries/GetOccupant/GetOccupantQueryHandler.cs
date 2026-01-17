using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Occupants;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupant;

public class GetOccupantQueryHandler : IRequestHandler<GetOccupantQuery, Result<OccupantDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOccupantQueryHandler> _logger;

    public GetOccupantQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOccupantQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OccupantDetailDto>> Handle(GetOccupantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var occupantId))
            {
                return Result.Failure<OccupantDetailDto>($"Invalid occupant ID format: {request.Id}");
            }

            var occupant = await _unitOfWork.Occupants.GetByIdAsync(occupantId, cancellationToken);

            if (occupant == null)
            {
                return Result.Failure<OccupantDetailDto>($"Occupant with ID {request.Id} not found");
            }

            // ✅ Map to OccupantDetailDto with all fields
            var occupantDto = new OccupantDetailDto
            {
                Id = occupant.Id,
                Code = occupant.Code,
                FullName = occupant.FullName,
                Email = occupant.Email,
                Phone = occupant.Phone,
                DateOfBirth = occupant.DateOfBirth,
                Status = occupant.Status.ToString(),
                ActiveContracts = 0,
                MoveInDate = occupant.MoveInDate,
                CreatedAt = occupant.CreatedAt,
                PropertyId = occupant.PropertyId,
                PropertyCode = occupant.PropertyCode,
                HasIdentityDocument = false,
                
                // ✅ Detailed information
                Address = occupant.Address,
                City = occupant.City,
                PostalCode = occupant.PostalCode,
                Country = occupant.Country,
                Nationality = occupant.Nationality,
                IdNumber = occupant.IdNumber,
                EmergencyContact = occupant.EmergencyContact,
                EmergencyPhone = occupant.EmergencyPhone,
                Occupation = occupant.Occupation,
                MonthlyIncome = occupant.MonthlyIncome,
                Notes = occupant.Notes
            };

            var hasIdentityDocument = await _unitOfWork.Documents.Query()
                .AnyAsync(d => !d.IsArchived
                               && d.AssociatedOccupantId == occupantId
                               && d.Type == DocumentType.PieceIdentite,
                    cancellationToken);

            occupantDto.HasIdentityDocument = hasIdentityDocument;

            return Result.Success(occupantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving occupant with ID {OccupantId}", request.Id);
            return Result.Failure<OccupantDetailDto>("Error retrieving occupant");
        }
    }
}
