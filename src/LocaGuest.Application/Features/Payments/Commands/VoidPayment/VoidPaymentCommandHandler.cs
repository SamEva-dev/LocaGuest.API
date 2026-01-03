using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Payments.Commands.VoidPayment;

public class VoidPaymentCommandHandler : IRequestHandler<VoidPaymentCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<VoidPaymentCommandHandler> _logger;

    public VoidPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<VoidPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Result<bool>.Failure<bool>("User not authenticated");
        }

        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId, cancellationToken);

            if (payment == null)
            {
                return Result<bool>.Failure<bool>("Payment not found");
            }

            // Mark payment as voided
            payment.MarkAsVoided();

            // Save changes
            var saved = await _unitOfWork.CommitAsync(cancellationToken);

            if (saved == 0)
            {
                return Result<bool>.Failure<bool>("Failed to void payment");
            }

            _logger.LogInformation(
                "Payment {PaymentId} voided. Reason: {Reason}", 
                request.PaymentId, 
                request.Reason ?? "No reason provided");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding payment {PaymentId}", request.PaymentId);
            return Result<bool>.Failure<bool>("Failed to void payment");
        }
    }
}
