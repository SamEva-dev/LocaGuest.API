using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Occupants;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Commands.CreateOccupant;

public class CreateOccupantCommandHandler : IRequestHandler<CreateOccupantCommand, Result<OccupantDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly ILogger<CreateOccupantCommandHandler> _logger;

    public CreateOccupantCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        INumberSequenceService numberSequenceService,
        ILogger<CreateOccupantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _numberSequenceService = numberSequenceService;
        _logger = logger;
    }

    public async Task<Result<OccupantDetailDto>> Handle(CreateOccupantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_orgContext.IsAuthenticated)
                return Result.Failure<OccupantDetailDto>("User not authenticated");

            // Create full name
            var fullName = $"{request.FirstName} {request.LastName}".Trim();

            // ✅ QUICK WIN: Generate automatic code
            var code = await _numberSequenceService.GenerateNextCodeAsync(
                _orgContext.OrganizationId!.Value,
                EntityPrefixes.Occupant,
                cancellationToken);

            _logger.LogInformation("Generated code for new occupant: {Code}", code);

            // Create occupant entity using factory method
            var occupant = Occupant.Create(fullName, request.Email, request.Phone);

            // ✅ Set the generated code
            occupant.SetCode(code);
            
            // ✅ Set detailed information
            occupant.UpdateDetails(
                dateOfBirth: request.DateOfBirth,
                address: request.Address,
                city: request.City,
                postalCode: request.PostalCode,
                country: request.Country,
                nationality: request.Nationality,
                idNumber: request.IdNumber,
                emergencyContact: request.EmergencyContact,
                emergencyPhone: request.EmergencyPhone,
                occupation: request.Occupation,
                monthlyIncome: request.MonthlyIncome,
                notes: request.Notes
            );

            // Add to context
            await _unitOfWork.Occupants.AddAsync(occupant, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Occupant created successfully: {OccupantId} - {OccupantName}", occupant.Id, occupant.FullName);

            // Map to DTO
            var dto = new OccupantDetailDto
            {
                Id = occupant.Id,
                Code = occupant.Code,  // ✅ Include generated code
                FullName = occupant.FullName,
                Email = occupant.Email,
                Phone = occupant.Phone,
                Status = occupant.Status.ToString(),
                ActiveContracts = 0,
                MoveInDate = occupant.MoveInDate,
                CreatedAt = occupant.CreatedAt,
                Address = request.Address,
                City = request.City,
                PostalCode = request.PostalCode,
                Country = request.Country,
                Nationality = request.Nationality,
                IdNumber = request.IdNumber,
                EmergencyContact = request.EmergencyContact,
                EmergencyPhone = request.EmergencyPhone,
                Occupation = request.Occupation,
                MonthlyIncome = request.MonthlyIncome,
                Notes = request.Notes,
                DateOfBirth = request.DateOfBirth
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating occupant");
            return Result.Failure<OccupantDetailDto>($"Error creating occupant: {ex.Message}");
        }
    }
}
