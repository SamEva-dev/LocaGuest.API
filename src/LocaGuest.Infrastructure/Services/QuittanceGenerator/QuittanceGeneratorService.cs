using LocaGuest.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.QuittanceGenerator;

public class QuittanceGeneratorService : IQuittanceGeneratorService
{
    private readonly ILogger<QuittanceGeneratorService> _logger;

    public QuittanceGeneratorService(ILogger<QuittanceGeneratorService> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> GenerateQuittancePdfAsync(
        string tenantName,
        string tenantEmail,
        string propertyName,
        string propertyAddress,
        string propertyCity,
        decimal amount,
        DateTime paymentDate,
        string month,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Column(column =>
                    {
                        column.Item().Text("QUITTANCE DE LOYER")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                    });

                    page.Content().PaddingTop(20).Column(column =>
                    {
                        // Info propriétaire
                        column.Item().Text($"De: {propertyName}").FontSize(12);
                        column.Item().Text($"Adresse: {propertyAddress}, {propertyCity}").FontSize(10);
                        
                        column.Item().PaddingTop(20);
                        
                        // Info locataire
                        column.Item().Text($"À: {tenantName}").FontSize(12);
                        column.Item().Text($"Email: {tenantEmail}").FontSize(10);
                        
                        column.Item().PaddingTop(30);
                        
                        // Corps
                        column.Item().Text($"Je soussigné(e), propriétaire du bien situé au {propertyAddress}, {propertyCity}, certifie avoir reçu de {tenantName} la somme de:")
                            .FontSize(11);
                        
                        column.Item().PaddingTop(15).Text($"{amount:N2} €")
                            .FontSize(18).Bold().FontColor(Colors.Green.Darken1);
                        
                        column.Item().PaddingTop(5).Text($"Au titre du loyer du mois de {month}")
                            .FontSize(11);
                        
                        column.Item().PaddingTop(20);
                        
                        // Date
                        column.Item().Text($"Fait le {paymentDate:dd/MM/yyyy}")
                            .FontSize(10);
                        
                        if (!string.IsNullOrEmpty(reference))
                        {
                            column.Item().PaddingTop(10).Text($"Référence: {reference}")
                                .FontSize(9).Italic();
                        }
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Généré le ").FontSize(8);
                        text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                    });
                });
            }).GeneratePdf();

            _logger.LogInformation("Quittance PDF generated for {TenantName}, amount {Amount}", 
                tenantName, amount);

            return Task.FromResult(pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quittance PDF");
            throw;
        }
    }
}
