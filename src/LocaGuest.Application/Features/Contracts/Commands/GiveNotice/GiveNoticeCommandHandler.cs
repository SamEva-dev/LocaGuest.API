using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.GiveNotice;

public class GiveNoticeCommandHandler : IRequestHandler<GiveNoticeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GiveNoticeCommandHandler> _logger;

    public GiveNoticeCommandHandler(IUnitOfWork unitOfWork, ILogger<GiveNoticeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(GiveNoticeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            contract.GiveNotice(request.NoticeDate, request.NoticeEndDate, request.Reason);

            _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Notice registered for contract {ContractId} until {NoticeEndDate}", request.ContractId, request.NoticeEndDate);
            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error giving notice for contract {ContractId}", request.ContractId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error giving notice for contract {ContractId}", request.ContractId);
            return Result.Failure($"Error giving notice: {ex.Message}");
        }
    }
}
