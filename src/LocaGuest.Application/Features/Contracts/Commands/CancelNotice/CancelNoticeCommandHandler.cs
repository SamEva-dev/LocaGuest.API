using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Commands.CancelNotice;

public class CancelNoticeCommandHandler : IRequestHandler<CancelNoticeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelNoticeCommandHandler> _logger;

    public CancelNoticeCommandHandler(IUnitOfWork unitOfWork, ILogger<CancelNoticeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelNoticeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(request.ContractId, cancellationToken);
            if (contract == null)
                return Result.Failure("Contract not found");

            contract.CancelNotice();
            _unitOfWork.Contracts.Update(contract);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Notice cancelled for contract {ContractId}", request.ContractId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling notice for contract {ContractId}", request.ContractId);
            return Result.Failure($"Error cancelling notice: {ex.Message}");
        }
    }
}
