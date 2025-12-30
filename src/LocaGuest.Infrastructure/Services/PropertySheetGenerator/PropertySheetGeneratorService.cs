using LocaGuest.Application.Interfaces;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.PropertySheetGenerator;

public class PropertySheetGeneratorService : IPropertySheetGeneratorService
{
    private readonly ILogger<PropertySheetGeneratorService> _logger;

    public PropertySheetGeneratorService(ILogger<PropertySheetGeneratorService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GeneratePropertySheetPdfAsync(
        Property property,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating property sheet PDF for Property={PropertyId}", property.Id);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, "FICHE BIEN", currentUserFullName, currentUserEmail, currentUserPhone));
                page.Content().Element(c => ComposeContent(c, property));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return Task.Run(() => document.GeneratePdf(), cancellationToken);
    }

    private static void ComposeHeader(IContainer container, string title, string userName, string userEmail, string? userPhone)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("LocaGuest").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().Text(title).FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2);
                    c.Item().PaddingTop(4).Text($"Éditée le {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(220).AlignRight().Column(c =>
                {
                    c.Item().Text("Conseiller / Agence").FontSize(9).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(userName).SemiBold();
                    c.Item().Text(userEmail).FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(userPhone))
                        c.Item().Text(userPhone).FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, Property property)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Element(c => ComposeKeyFacts(c, property));
            col.Item().Element(c => ComposeAddress(c, property));
            col.Item().Element(c => ComposeTechnical(c, property));

            if (!string.IsNullOrWhiteSpace(property.Description))
            {
                col.Item().Element(c => ComposeNotes(c, property.Description!));
            }
        });
    }

    private static void ComposeCard(IContainer container, string title, Action<IContainer> content)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(12)
            .Column(col =>
            {
                col.Item().Text(title).Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                col.Item().PaddingTop(6).Element(content);
            });
    }

    private static void ComposeKeyFacts(IContainer container, Property property)
    {
        ComposeCard(container, "Informations clés", c =>
        {
            c.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Nom : {property.Name}").SemiBold();
                    col.Item().Text($"Code : {property.Code}").FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"Statut : {property.Status}").FontColor(Colors.Grey.Darken2);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Loyer : {property.Rent:0.##} €").Bold().FontSize(12).FontColor(Colors.Green.Darken2);
                    col.Item().Text($"Charges : {(property.Charges ?? 0):0.##} €");
                    col.Item().Text($"Dépôt : {(property.Deposit ?? 0):0.##} €");
                });
            });
        });
    }

    private static void ComposeAddress(IContainer container, Property property)
    {
        ComposeCard(container, "Adresse", c =>
        {
            c.Column(col =>
            {
                col.Item().Text(property.Address).SemiBold();
                col.Item().Text($"{property.PostalCode ?? ""} {property.City}");
                if (!string.IsNullOrWhiteSpace(property.Country))
                    col.Item().Text(property.Country!).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void ComposeTechnical(IContainer container, Property property)
    {
        ComposeCard(container, "Caractéristiques", c =>
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                void Row(string label, string value)
                {
                    table.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken2);
                    table.Cell().PaddingVertical(2).Text(value).SemiBold();
                }

                Row("Type", property.Type.ToString());
                Row("Usage", property.UsageType.ToString());
                Row("Surface", property.Surface.HasValue ? $"{property.Surface:0.##} m²" : "-");
                Row("Meublé", property.IsFurnished ? "Oui" : "Non");
                Row("Chambres", property.Bedrooms.ToString());
                Row("Salles de bain", property.Bathrooms.ToString());
                Row("Étage", property.Floor.HasValue ? property.Floor.Value.ToString() : "-");
                Row("Ascenseur", property.HasElevator ? "Oui" : "Non");
                Row("Parking", property.HasParking ? "Oui" : "Non");

                if (property.TotalRooms.HasValue)
                    Row("Total pièces (colocation)", property.TotalRooms.Value.ToString());
            });
        });
    }

    private static void ComposeNotes(IContainer container, string notes)
    {
        ComposeCard(container, "Notes", c =>
        {
            c.Text(notes).FontColor(Colors.Grey.Darken3);
        });
    }
}
