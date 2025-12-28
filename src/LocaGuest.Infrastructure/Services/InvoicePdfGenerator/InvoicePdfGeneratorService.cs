using LocaGuest.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.InvoicePdfGenerator;

public class InvoicePdfGeneratorService : IInvoicePdfGeneratorService
{
    private readonly ILogger<InvoicePdfGeneratorService> _logger;

    public InvoicePdfGeneratorService(ILogger<InvoicePdfGeneratorService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateInvoicePdfAsync(
        string invoiceNumber,
        string tenantName,
        string? tenantEmail,
        string propertyName,
        string? propertyAddress,
        string? propertyCity,
        int month,
        int year,
        DateTime dueDate,
        decimal totalAmount,
        IReadOnlyCollection<(string label, decimal amount)> lines,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var monthLabel = new DateTime(year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("FACTURE DE LOYER").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                left.Item().Text($"Période: {monthLabel}").FontSize(11).FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(220).Column(right =>
                            {
                                right.Item().AlignRight().Text($"N° {invoiceNumber}").FontSize(12).Bold();
                                right.Item().AlignRight().Text($"Échéance: {dueDate:dd/MM/yyyy}").FontSize(10);
                                right.Item().AlignRight().Text($"Généré le: {DateTime.UtcNow:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                        });

                        column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                    });

                    page.Content().PaddingTop(18).Column(column =>
                    {
                        column.Spacing(12);

                        // Bien / Bailleur (placeholder)
                        column.Item().Column(c =>
                        {
                            c.Item().Text("Bien concerné").FontSize(11).Bold();
                            c.Item().Text(propertyName).FontSize(10);
                            if (!string.IsNullOrWhiteSpace(propertyAddress) || !string.IsNullOrWhiteSpace(propertyCity))
                            {
                                c.Item().Text($"{propertyAddress ?? ""} {propertyCity ?? ""}".Trim()).FontSize(9).FontColor(Colors.Grey.Darken2);
                            }
                        });

                        // Locataire
                        column.Item().Column(c =>
                        {
                            c.Item().Text("Locataire").FontSize(11).Bold();
                            c.Item().Text(tenantName).FontSize(10);
                            if (!string.IsNullOrWhiteSpace(tenantEmail))
                                c.Item().Text(tenantEmail).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });

                        // Détails
                        column.Item().Text("Détail de la facture").FontSize(11).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeaderStyle).Text("Libellé").FontSize(10).Bold();
                                header.Cell().Element(CellHeaderStyle).AlignRight().Text("Montant").FontSize(10).Bold();
                            });

                            foreach (var l in lines)
                            {
                                table.Cell().Element(CellRowStyle).Text(l.label).FontSize(10);
                                table.Cell().Element(CellRowStyle).AlignRight().Text($"{l.amount:N2} €").FontSize(10);
                            }

                            table.Cell().Element(CellTotalStyle).Text("Total").FontSize(10).Bold();
                            table.Cell().Element(CellTotalStyle).AlignRight().Text($"{totalAmount:N2} €").FontSize(10).Bold();
                        });

                        column.Item().PaddingTop(8).Text("Merci de régler avant la date d'échéance indiquée.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Document généré automatiquement - ").FontSize(8).FontColor(Colors.Grey.Darken2);
                        text.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                    });
                });
            }).GeneratePdf();

            _logger.LogInformation("Invoice PDF generated: {InvoiceNumber} for {TenantName}", invoiceNumber, tenantName);
            return Task.FromResult(pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF");
            throw;
        }
    }

    private static IContainer CellHeaderStyle(IContainer container)
        => container.DefaultTextStyle(x => x.FontColor(Colors.White))
            .Background(Colors.Blue.Darken2)
            .PaddingVertical(6)
            .PaddingHorizontal(8);

    private static IContainer CellRowStyle(IContainer container)
        => container.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(6)
            .PaddingHorizontal(8);

    private static IContainer CellTotalStyle(IContainer container)
        => container.Background(Colors.Grey.Lighten4)
            .PaddingVertical(7)
            .PaddingHorizontal(8);
}
