using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Inventories.Queries.GenerateInventoryPdf;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail;

public class SendInventoryEmailCommandHandler : IRequestHandler<SendInventoryEmailCommand, Result>
{
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendInventoryEmailCommandHandler> _logger;

    public SendInventoryEmailCommandHandler(
        IMediator mediator,
        IEmailService emailService,
        ILogger<SendInventoryEmailCommandHandler> logger)
    {
        _mediator = mediator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(SendInventoryEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Générer le PDF
            var pdfQuery = new GenerateInventoryPdfQuery
            {
                InventoryId = request.InventoryId,
                InventoryType = request.InventoryType
            };

            var pdfResult = await _mediator.Send(pdfQuery, cancellationToken);
            if (!pdfResult.IsSuccess || pdfResult.Data == null)
                return Result.Failure($"Failed to generate PDF: {pdfResult.ErrorMessage}");

            // Préparer l'email
            var subject = request.InventoryType == "Entry" 
                ? "État des lieux d'entrée - LocaGuest"
                : "État des lieux de sortie - LocaGuest";

            var body = $@"
                <h2>Bonjour {request.RecipientName},</h2>
                <p>Vous trouverez ci-joint votre <strong>état des lieux {(request.InventoryType == "Entry" ? "d'entrée" : "de sortie")}</strong>.</p>
                <p>Ce document fait partie intégrante de votre contrat de location.</p>
                <p>Merci de le consulter attentivement et de nous faire part de toute remarque dans les meilleurs délais.</p>
                <br/>
                <p>Cordialement,<br/>L'équipe LocaGuest</p>
            ";

            var fileName = request.InventoryType == "Entry" 
                ? $"EDL_Entree_{DateTime.Now:yyyyMMdd}.pdf"
                : $"EDL_Sortie_{DateTime.Now:yyyyMMdd}.pdf";

            var attachments = new List<EmailAttachment>
            {
                new EmailAttachment
                {
                    FileName = fileName,
                    Content = pdfResult.Data,
                    ContentType = "application/pdf"
                }
            };

            // Envoyer l'email
            await _emailService.SendEmailAsync(
                request.RecipientEmail,
                subject,
                body,
                attachments,
                cancellationToken);

            _logger.LogInformation("Inventory email sent to {Email} for inventory {InventoryId}", 
                request.RecipientEmail, request.InventoryId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending inventory email for {InventoryId}", request.InventoryId);
            return Result.Failure($"Error sending email: {ex.Message}");
        }
    }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}
