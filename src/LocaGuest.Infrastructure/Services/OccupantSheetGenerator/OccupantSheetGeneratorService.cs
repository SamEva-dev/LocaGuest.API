using LocaGuest.Application.Interfaces;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.OccupantSheetGenerator;

public class OccupantSheetGeneratorService : IOccupantSheetGeneratorService
{
    private readonly ILogger<OccupantSheetGeneratorService> _logger;

    public OccupantSheetGeneratorService(ILogger<OccupantSheetGeneratorService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateOccupantSheetPdfAsync(
        Occupant occupant,
        Property? associatedProperty,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating occupant sheet PDF for Occupant={OccupantId}", occupant.Id);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, "FICHE OCCUPANT", currentUserFullName, currentUserEmail, currentUserPhone));
                page.Content().Element(c => ComposeContent(c, occupant, associatedProperty));
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
                    c.Item().Text("LocaGuest").FontSize(18).Bold().FontColor(Colors.Orange.Darken3);
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

    private static void ComposeContent(IContainer container, Occupant occupant, Property? property)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Element(c => ComposeIdentity(c, occupant));
            col.Item().Element(c => ComposeContact(c, occupant));
            col.Item().Element(c => ComposeAddress(c, occupant));

            if (property != null)
            {
                col.Item().Element(c => ComposeAssociatedProperty(c, occupant, property));
            }

            col.Item().Element(c => ComposeProfessional(c, occupant));

            if (!string.IsNullOrWhiteSpace(occupant.Notes))
                col.Item().Element(c => ComposeNotes(c, occupant.Notes!));
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
                col.Item().Text(title).Bold().FontSize(12).FontColor(Colors.Orange.Darken3);
                col.Item().PaddingTop(6).Element(content);
            });
    }

    private static void ComposeIdentity(IContainer container, Occupant occupant)
    {
        ComposeCard(container, "Identité", c =>
        {
            c.Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                void Row(string label, string value)
                {
                    t.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken2);
                    t.Cell().PaddingVertical(2).Text(value).SemiBold();
                }

                Row("Nom", occupant.FullName);
                Row("Code", occupant.Code);
                Row("Statut", occupant.Status.ToString());
                Row("Date d'entrée", occupant.MoveInDate.HasValue ? occupant.MoveInDate.Value.ToString("dd/MM/yyyy") : "-");
                Row("Date de naissance", occupant.DateOfBirth.HasValue ? occupant.DateOfBirth.Value.ToString("dd/MM/yyyy") : "-");
                Row("Nationalité", occupant.Nationality ?? "-");
                Row("Pièce d'identité", occupant.IdNumber ?? "-");
            });
        });
    }

    private static void ComposeContact(IContainer container, Occupant occupant)
    {
        ComposeCard(container, "Contact", c =>
        {
            c.Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                void Row(string label, string value)
                {
                    t.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken2);
                    t.Cell().PaddingVertical(2).Text(value).SemiBold();
                }

                Row("Email", occupant.Email);
                Row("Téléphone", occupant.Phone ?? "-");
                Row("Contact d'urgence", occupant.EmergencyContact ?? "-");
                Row("Téléphone urgence", occupant.EmergencyPhone ?? "-");
            });
        });
    }

    private static void ComposeAddress(IContainer container, Occupant occupant)
    {
        if (string.IsNullOrWhiteSpace(occupant.Address) && string.IsNullOrWhiteSpace(occupant.City) && string.IsNullOrWhiteSpace(occupant.PostalCode) && string.IsNullOrWhiteSpace(occupant.Country))
            return;

        ComposeCard(container, "Adresse", c =>
        {
            c.Column(cc =>
            {
                if (!string.IsNullOrWhiteSpace(occupant.Address))
                    cc.Item().Text(occupant.Address!).SemiBold();

                var cityLine = $"{occupant.PostalCode ?? ""} {occupant.City ?? ""}".Trim();
                if (!string.IsNullOrWhiteSpace(cityLine))
                    cc.Item().Text(cityLine);

                if (!string.IsNullOrWhiteSpace(occupant.Country))
                    cc.Item().Text(occupant.Country!).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void ComposeAssociatedProperty(IContainer container, Occupant occupant, Property property)
    {
        ComposeCard(container, "Bien associé", c =>
        {
            c.Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                void Row(string label, string value)
                {
                    t.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken2);
                    t.Cell().PaddingVertical(2).Text(value).SemiBold();
                }

                Row("Bien", property.Name);
                Row("Code", property.Code);
                Row("Adresse", property.Address);
                Row("Ville", $"{property.PostalCode ?? ""} {property.City}".Trim());
                Row("Référence", occupant.PropertyCode ?? "-");
            });
        });
    }

    private static void ComposeProfessional(IContainer container, Occupant occupant)
    {
        if (string.IsNullOrWhiteSpace(occupant.Occupation) && !occupant.MonthlyIncome.HasValue)
            return;

        ComposeCard(container, "Situation professionnelle", c =>
        {
            c.Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                void Row(string label, string value)
                {
                    t.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken2);
                    t.Cell().PaddingVertical(2).Text(value).SemiBold();
                }

                Row("Profession", occupant.Occupation ?? "-");
                Row("Revenu mensuel", occupant.MonthlyIncome.HasValue ? $"{occupant.MonthlyIncome:0.##} €" : "-");
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
