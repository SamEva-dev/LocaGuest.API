using System.Text.Json;
using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Addendums.Commands.MarkAddendumAsSigned;

public class MarkAddendumAsSignedCommandHandler : IRequestHandler<MarkAddendumAsSignedCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkAddendumAsSignedCommandHandler> _logger;

    public MarkAddendumAsSignedCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkAddendumAsSignedCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(MarkAddendumAsSignedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var addendum = await _unitOfWork.Addendums.GetByIdAsync(request.AddendumId, cancellationToken);
            if (addendum == null)
                return Result.Failure<Guid>("Addendum not found");

            if (addendum.SignatureStatus == AddendumSignatureStatus.Rejected)
                return Result.Failure<Guid>("Rejected addendum cannot be signed");

            if (addendum.SignatureStatus != AddendumSignatureStatus.Signed)
            {
                addendum.MarkAsSigned(request.SignedDateUtc);
            }

            // Mark associated Avenant document(s) as signed
            var docIds = new List<Guid>();
            if (!string.IsNullOrWhiteSpace(addendum.AttachedDocumentIds))
            {
                try
                {
                    docIds = JsonSerializer.Deserialize<List<Guid>>(addendum.AttachedDocumentIds) ?? new List<Guid>();
                }
                catch
                {
                    docIds = new List<Guid>();
                }
            }

            var signedDate = request.SignedDateUtc ?? DateTime.UtcNow;

            foreach (var docId in docIds)
            {
                var doc = await _unitOfWork.Documents.GetByIdAsync(docId, cancellationToken);
                if (doc == null)
                    continue;

                if (doc.Type != DocumentType.Avenant)
                    continue;

                if (doc.Status == DocumentStatus.Draft)
                {
                    doc.MarkAsSigned(signedDate, request.SignedBy);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Addendum {AddendumId} marked as signed", addendum.Id);

            return Result.Success(addendum.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing addendum {AddendumId}", request.AddendumId);
            return Result.Failure<Guid>($"Error signing addendum: {ex.Message}");
        }
    }
}
