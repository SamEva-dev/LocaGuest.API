using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Application.Interfaces;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.ContractGenerator;

public class ContractGeneratorService : IContractGeneratorService
{
    private readonly ILogger<ContractGeneratorService> _logger;

    public ContractGeneratorService(ILogger<ContractGeneratorService> logger)
    {
        _logger = logger;
        
        // QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateContractPdfAsync(
        Tenant tenant,
        Property property,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        GenerateContractDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating contract: Type={ContractType}, Tenant={TenantId}, Property={PropertyId}",
            dto.ContractType,
            dto.TenantId,
            dto.PropertyId);

        var model = BuildContractModel(tenant, property, currentUserFullName, currentUserEmail, currentUserPhone, dto);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, model));
                page.Content().Element(c => ComposeContent(c, model, dto));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return await Task.Run(() => document.GeneratePdf(), cancellationToken);
    }

    private void ComposeHeader(IContainer container, ContractModel model)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().PaddingBottom(20).Column(c =>
            {
                c.Item().Text("CONTRAT DE LOCATION")
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);
                
                c.Item().Text($"{model.PropertyTypeLabel} - {model.ContractType}")
                    .FontSize(12)
                    .FontColor(Colors.Grey.Darken2);
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, ContractModel model, GenerateContractDto dto)
    {
        container.Column(column =>
        {
            // Parties
            column.Item().Element(c => ComposeParties(c, model, dto));

            // Objet du contrat
            column.Item().PaddingTop(15).Element(c => ComposePropertyDetails(c, model));

            // Durée
            column.Item().PaddingTop(15).Element(c => ComposeDuration(c, model));

            // Conditions financières
            column.Item().PaddingTop(15).Element(c => ComposeFinancialTerms(c, model, dto));

            // Clauses légales
            column.Item().PageBreak();
            column.Item().Element(c => ComposeLegalClauses(c, model, dto));

            // Signatures
            column.Item().PaddingTop(30).Element(ComposeSignatures);
        });
    }

    private void ComposeParties(IContainer container, ContractModel model, GenerateContractDto dto)
    {
        container.Column(column =>
        {
            column.Item().Text("ENTRE LES SOUSSIGNÉS").Bold().FontSize(14);

            // Bailleur
            column.Item().PaddingTop(10).Column(c =>
            {
                c.Item().Text("LE BAILLEUR").Bold().FontSize(12);
                
                if (dto.IsThirdPartyLandlord && dto.LandlordInfo != null)
                {
                    var ll = dto.LandlordInfo;
                    if (!string.IsNullOrEmpty(ll.CompanyName))
                        c.Item().Text($"Raison sociale: {ll.CompanyName}");
                    if (!string.IsNullOrEmpty(ll.FirstName) || !string.IsNullOrEmpty(ll.LastName))
                        c.Item().Text($"Représenté par: {ll.FirstName} {ll.LastName}");
                    if (!string.IsNullOrEmpty(ll.Address))
                        c.Item().Text($"Adresse: {ll.Address}");
                    if (!string.IsNullOrEmpty(ll.Siret))
                        c.Item().Text($"SIRET: {ll.Siret}");
                    if (!string.IsNullOrEmpty(ll.Email))
                        c.Item().Text($"Email: {ll.Email}");
                    if (!string.IsNullOrEmpty(ll.Phone))
                        c.Item().Text($"Téléphone: {ll.Phone}");
                }
                else
                {
                    c.Item().Text($"Nom: {model.CurrentUserFullName}");
                    c.Item().Text($"Email: {model.CurrentUserEmail}");
                    if (!string.IsNullOrEmpty(model.CurrentUserPhone))
                        c.Item().Text($"Téléphone: {model.CurrentUserPhone}");
                }
            });

            // Locataire
            column.Item().PaddingTop(10).Column(c =>
            {
                c.Item().Text("LE LOCATAIRE").Bold().FontSize(12);
                c.Item().Text($"Nom: {model.TenantFullName}");
                if (!string.IsNullOrEmpty(model.TenantBirthDate))
                    c.Item().Text($"Date de naissance: {model.TenantBirthDate}");
                c.Item().Text($"Email: {model.TenantEmail}");
                if (!string.IsNullOrEmpty(model.TenantPhone))
                    c.Item().Text($"Téléphone: {model.TenantPhone}");
            });
        });
    }

    private void ComposePropertyDetails(IContainer container, ContractModel model)
    {
        container.Column(column =>
        {
            column.Item().Text("OBJET DU CONTRAT").Bold().FontSize(14);
            column.Item().PaddingTop(5).Text("Le bailleur loue au locataire le bien suivant :");

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                });

                AddTableRow(table, "Adresse", $"{model.PropertyAddress}, {model.PropertyPostalCode} {model.PropertyCity}");
                AddTableRow(table, "Type de bien", model.PropertyTypeLabel);
                AddTableRow(table, "Surface", $"{model.PropertySurface} m²");
                AddTableRow(table, "Nombre de pièces", $"{model.PropertyBedrooms} chambres");
                
                if (model.PropertyFloor.HasValue)
                {
                    var floorText = model.PropertyElevator ? $"{model.PropertyFloor} (avec ascenseur)" : model.PropertyFloor.ToString();
                    AddTableRow(table, "Étage", floorText!);
                }
            });
        });
    }

    private void ComposeDuration(IContainer container, ContractModel model)
    {
        container.Column(column =>
        {
            column.Item().Text("DURÉE DU CONTRAT").Bold().FontSize(14);
            column.Item().PaddingTop(5).Text(text =>
            {
                text.Span("Le présent contrat est conclu pour une durée de ");
                text.Span($"{model.ContractDurationMonths} mois").Bold();
                text.Span($", du ");
                text.Span(model.StartDate).Bold();
                text.Span(" au ");
                text.Span(model.EndDate).Bold();
                text.Span(".");
            });
        });
    }

    private void ComposeFinancialTerms(IContainer container, ContractModel model, GenerateContractDto dto)
    {
        container.Column(column =>
        {
            column.Item().Text("CONDITIONS FINANCIÈRES").Bold().FontSize(14);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(120);
                });

                AddTableRow(table, "Loyer mensuel hors charges", $"{model.Rent} €");
                
                if (dto.Charges.HasValue && dto.Charges > 0)
                {
                    AddTableRow(table, "Charges mensuelles", $"{model.Charges} €");
                    AddTableRow(table, "Loyer charges comprises", $"{model.TotalRent} €");
                }
                
                if (dto.Deposit.HasValue && dto.Deposit > 0)
                {
                    AddTableRow(table, "Dépôt de garantie", $"{model.Deposit} €");
                }
            });
        });
    }

    private void ComposeLegalClauses(IContainer container, ContractModel model, GenerateContractDto dto)
    {
        container.Column(column =>
        {
            column.Item().Text("CLAUSES LÉGALES").Bold().FontSize(14);

            AddClause(column, "Article 1 - Usage du logement",
                "Les locaux loués sont à usage d'habitation principale uniquement conformément à la loi n° 89-462 du 6 juillet 1989.");

            AddClause(column, "Article 2 - État du logement",
                "Le bailleur s'engage à remettre au locataire un logement décent ne laissant pas apparaître de risques manifestes pouvant porter atteinte à la sécurité physique ou à la santé, conforme aux critères fixés par décret.");

            AddClause(column, "Article 3 - Travaux",
                "Sont à la charge du locataire les réparations locatives définies par le décret n° 87-712 du 26 août 1987, notamment l'entretien courant du logement.");

            AddClause(column, "Article 4 - Paiement du loyer",
                $"Le loyer est payable mensuellement à terme échu, le premier jour de chaque mois. Le premier paiement sera effectué le {model.FirstPaymentDate}.");

            AddClause(column, "Article 5 - Révision du loyer",
                "Le loyer pourra être révisé annuellement selon l'Indice de Référence des Loyers (IRL) publié par l'INSEE, conformément à l'article 17c de la loi du 6 juillet 1989.");

            AddClause(column, "Article 6 - Charges récupérables",
                "Les charges récupérables sont celles prévues par le décret n° 87-713 du 26 août 1987, notamment : eau froide et chaude, chauffage collectif, taxe d'enlèvement des ordures ménagères, etc.");

            AddClause(column, "Article 7 - Dépôt de garantie",
                $"Le dépôt de garantie, d'un montant de {model.Deposit} €, sera restitué dans un délai d'un mois (ou deux mois si état des lieux de sortie différent de celui d'entrée) à compter de la remise des clés par le locataire.");

            AddClause(column, "Article 8 - Assurance",
                "Le locataire s'engage à souscrire une assurance contre les risques locatifs (incendie, dégât des eaux, explosion) et à en justifier lors de la remise des clés puis annuellement.");

            AddClause(column, "Article 9 - Congé",
                "Le locataire peut donner congé à tout moment avec un préavis de 3 mois (1 mois en zone tendue). Le bailleur peut donner congé uniquement à l'échéance du bail avec un préavis de 6 mois pour les motifs légaux (vente, reprise, motif légitime et sérieux).");

            AddClause(column, "Article 10 - Diagnostic de performance énergétique (DPE)",
                $"Classe énergétique : {model.EnergyClass}");

            if (!string.IsNullOrEmpty(dto.AdditionalClauses))
            {
                AddClause(column, "Article 11 - Clauses particulières", dto.AdditionalClauses);
            }
        });
    }

    private void AddClause(ColumnDescriptor column, string title, string content)
    {
        column.Item().PaddingTop(10).Column(c =>
        {
            c.Item().Text(title).Bold().FontSize(11);
            c.Item().PaddingTop(3).PaddingLeft(15).Text(content).FontSize(10);
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Le Bailleur").Bold();
                c.Item().PaddingTop(5).Text($"Date : {DateTime.Now:dd/MM/yyyy}");
                c.Item().PaddingTop(30).Text("Signature précédée de la mention").FontSize(9);
                c.Item().Text("\"Lu et approuvé\"").FontSize(9).Italic();
            });

            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Le Locataire").Bold();
                c.Item().PaddingTop(5).Text($"Date : {DateTime.Now:dd/MM/yyyy}");
                c.Item().PaddingTop(30).Text("Signature précédée de la mention").FontSize(9);
                c.Item().Text("\"Lu et approuvé\"").FontSize(9).Italic();
            });
        });
    }

    private void AddTableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(label).Bold();
        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value);
    }

    private ContractModel BuildContractModel(
        Tenant tenant,
        Property property,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        GenerateContractDto dto)
    {
        var startDate = DateTime.Parse(dto.StartDate);
        var endDate = DateTime.Parse(dto.EndDate);
        var durationMonths = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;

        return new ContractModel
        {
            // Tenant
            TenantFullName = tenant.FullName,
            TenantEmail = tenant.Email,
            TenantPhone = tenant.Phone ?? "N/A",
            TenantBirthDate = null, // DateOfBirth not available in current Tenant entity

            // Property
            PropertyAddress = property.Address,
            PropertyCity = property.City,
            PropertyPostalCode = property.ZipCode ?? "N/A",
            PropertyType = property.Type.ToString(),
            PropertyTypeLabel = GetPropertyTypeLabel(property.Type.ToString()),
            PropertySurface = property.Surface ?? 0m,
            PropertyBedrooms = property.Bedrooms,
            PropertyFloor = property.Floor,
            PropertyElevator = property.HasElevator,
            EnergyClass = "N/A", // EnergyClass not available in current Property entity

            // Contract
            ContractType = dto.ContractType,
            StartDate = startDate.ToString("dd/MM/yyyy"),
            EndDate = endDate.ToString("dd/MM/yyyy"),
            ContractDurationMonths = durationMonths,
            Rent = dto.Rent.ToString("F2"),
            Charges = dto.Charges?.ToString("F2") ?? "0.00",
            TotalRent = (dto.Rent + (dto.Charges ?? 0)).ToString("F2"),
            Deposit = dto.Deposit?.ToString("F2") ?? "0.00",
            FirstPaymentDate = startDate.AddMonths(1).ToString("01/MM/yyyy"),

            // Current User
            CurrentUserFullName = currentUserFullName,
            CurrentUserEmail = currentUserEmail,
            CurrentUserPhone = currentUserPhone
        };
    }

    private string GetPropertyTypeLabel(string type)
    {
        return type switch
        {
            "Apartment" => "Appartement",
            "House" => "Maison",
            "Studio" => "Studio",
            "Duplex" => "Duplex",
            "Condo" => "Copropriété",
            _ => type
        };
    }
}

internal class ContractModel
{
    public string TenantFullName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public string? TenantBirthDate { get; set; }
    
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string PropertyPostalCode { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public decimal PropertySurface { get; set; }
    public int PropertyBedrooms { get; set; }
    public int? PropertyFloor { get; set; }
    public bool PropertyElevator { get; set; }
    public string EnergyClass { get; set; } = string.Empty;
    
    public string ContractType { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int ContractDurationMonths { get; set; }
    public string Rent { get; set; } = string.Empty;
    public string Charges { get; set; } = string.Empty;
    public string TotalRent { get; set; } = string.Empty;
    public string Deposit { get; set; } = string.Empty;
    public string FirstPaymentDate { get; set; } = string.Empty;
    
    public string CurrentUserFullName { get; set; } = string.Empty;
    public string CurrentUserEmail { get; set; } = string.Empty;
    public string? CurrentUserPhone { get; set; }
}
