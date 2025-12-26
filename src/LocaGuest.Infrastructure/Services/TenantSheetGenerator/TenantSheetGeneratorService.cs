using LocaGuest.Application.Interfaces;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.TenantSheetGenerator;

public class TenantSheetGeneratorService : ITenantSheetGeneratorService
{
    private readonly ILogger<TenantSheetGeneratorService> _logger;

    public TenantSheetGeneratorService(ILogger<TenantSheetGeneratorService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateTenantSheetPdfAsync(
        Tenant tenant,
        Property? associatedProperty,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tenant sheet PDF for Tenant={TenantId}", tenant.Id);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, "FICHE LOCATAIRE", currentUserFullName, currentUserEmail, currentUserPhone));
                page.Content().Element(c => ComposeContent(c, tenant, associatedProperty));
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

    private static void ComposeContent(IContainer container, Tenant tenant, Property? property)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Element(c => ComposeIdentity(c, tenant));
            col.Item().Element(c => ComposeContact(c, tenant));
            col.Item().Element(c => ComposeAddress(c, tenant));

            if (property != null)
            {
                col.Item().Element(c => ComposeAssociatedProperty(c, tenant, property));
            }

            col.Item().Element(c => ComposeProfessional(c, tenant));

            if (!string.IsNullOrWhiteSpace(tenant.Notes))
                col.Item().Element(c => ComposeNotes(c, tenant.Notes!));
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

    private static void ComposeIdentity(IContainer container, Tenant tenant)
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

                Row("Nom", tenant.FullName);
                Row("Code", tenant.Code);
                Row("Statut", tenant.Status.ToString());
                Row("Date d'entrée", tenant.MoveInDate.HasValue ? tenant.MoveInDate.Value.ToString("dd/MM/yyyy") : "-");
                Row("Date de naissance", tenant.DateOfBirth.HasValue ? tenant.DateOfBirth.Value.ToString("dd/MM/yyyy") : "-");
                Row("Nationalité", tenant.Nationality ?? "-");
                Row("Pièce d'identité", tenant.IdNumber ?? "-");
            });
        });
    }

    private static void ComposeContact(IContainer container, Tenant tenant)
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

                Row("Email", tenant.Email);
                Row("Téléphone", tenant.Phone ?? "-");
                Row("Contact d'urgence", tenant.EmergencyContact ?? "-");
                Row("Téléphone urgence", tenant.EmergencyPhone ?? "-");
            });
        });
    }

    private static void ComposeAddress(IContainer container, Tenant tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.Address) && string.IsNullOrWhiteSpace(tenant.City) && string.IsNullOrWhiteSpace(tenant.PostalCode) && string.IsNullOrWhiteSpace(tenant.Country))
            return;

        ComposeCard(container, "Adresse", c =>
        {
            c.Column(cc =>
            {
                if (!string.IsNullOrWhiteSpace(tenant.Address))
                    cc.Item().Text(tenant.Address!).SemiBold();

                var cityLine = $"{tenant.PostalCode ?? ""} {tenant.City ?? ""}".Trim();
                if (!string.IsNullOrWhiteSpace(cityLine))
                    cc.Item().Text(cityLine);

                if (!string.IsNullOrWhiteSpace(tenant.Country))
                    cc.Item().Text(tenant.Country!).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void ComposeAssociatedProperty(IContainer container, Tenant tenant, Property property)
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
                Row("Référence", tenant.PropertyCode ?? "-");
            });
        });
    }

    private static void ComposeProfessional(IContainer container, Tenant tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.Occupation) && !tenant.MonthlyIncome.HasValue)
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

                Row("Profession", tenant.Occupation ?? "-");
                Row("Revenu mensuel", tenant.MonthlyIncome.HasValue ? $"{tenant.MonthlyIncome:0.##} €" : "-");
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
