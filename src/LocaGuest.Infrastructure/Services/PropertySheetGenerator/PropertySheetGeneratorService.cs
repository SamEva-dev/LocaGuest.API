using LocaGuest.Application.Interfaces;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Infrastructure.Services.PropertySheetGenerator;

public class PropertySheetGeneratorService : IPropertySheetGeneratorService
{
    private readonly ILogger<PropertySheetGeneratorService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public PropertySheetGeneratorService(
        ILogger<PropertySheetGeneratorService> logger,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePropertySheetPdfAsync(
        Property property,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating property sheet PDF for Property={PropertyId}", property.Id);

        var photos = await LoadPropertyPhotosAsync(property, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, "FICHE BIEN", currentUserFullName, currentUserEmail, currentUserPhone));
                page.Content().Element(c => ComposeContent(c, property, photos));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private sealed class PropertySheetPhotos
    {
        public byte[]? CoverImage { get; init; }
        public IReadOnlyList<byte[]> GalleryImages { get; init; } = Array.Empty<byte[]>();
    }

    private async Task<PropertySheetPhotos> LoadPropertyPhotosAsync(Property property, CancellationToken cancellationToken)
    {
        try
        {
            if (property.ImageUrls == null || property.ImageUrls.Count == 0)
                return new PropertySheetPhotos();

            var orderedIds = property.ImageUrls
                .Select(x => Guid.TryParse(x, out var g) ? (Guid?)g : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (orderedIds.Count == 0)
                return new PropertySheetPhotos();

            var idSet = orderedIds.ToHashSet();

            var images = await _unitOfWork.PropertyImages
                .FindAsync(i => i.PropertyId == property.Id && idSet.Contains(i.Id), cancellationToken);

            var byId = images.ToDictionary(i => i.Id, i => i);

            var orderedImages = orderedIds
                .Where(id => byId.ContainsKey(id))
                .Select(id => byId[id])
                .ToList();

            var bytes = new List<byte[]>();
            foreach (var img in orderedImages)
            {
                if (string.IsNullOrWhiteSpace(img.FilePath))
                    continue;

                // Only embed images in PDF
                if (!img.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var b = await _fileStorage.ReadFileAsync(img.FilePath, cancellationToken);
                    if (b.Length > 0)
                        bytes.Add(b);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load image {ImageId} for property sheet", img.Id);
                }
            }

            if (bytes.Count == 0)
                return new PropertySheetPhotos();

            var cover = bytes[0];
            var gallery = bytes.Skip(1).Take(4).ToList();
            return new PropertySheetPhotos
            {
                CoverImage = cover,
                GalleryImages = gallery
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load photos for property sheet {PropertyId}", property.Id);
            return new PropertySheetPhotos();
        }
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

    private static void ComposePhotos(IContainer container, PropertySheetPhotos photos)
    {
        ComposeCard(container, "Photos", c =>
        {
            c.Column(col =>
            {
                col.Spacing(8);

                // Cover
                col.Item().Height(220).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4)
                    .AlignCenter().AlignMiddle().Image(photos.CoverImage!).FitArea();

                // Gallery (up to 4)
                if (photos.GalleryImages.Count > 0)
                {
                    col.Item().Row(row =>
                    {
                        row.Spacing(8);
                        foreach (var img in photos.GalleryImages)
                        {
                            row.RelativeItem().Height(90).Border(1).BorderColor(Colors.Grey.Lighten2)
                                .Background(Colors.Grey.Lighten4)
                                .AlignCenter().AlignMiddle().Image(img).FitArea();
                        }

                        // Fill remaining slots to keep a clean grid
                        for (var i = photos.GalleryImages.Count; i < 4; i++)
                        {
                            row.RelativeItem().Height(90).Border(1).BorderColor(Colors.Grey.Lighten2)
                                .Background(Colors.Grey.Lighten5);
                        }
                    });
                }
            });
        });
    }

    private static void ComposeContent(IContainer container, Property property, PropertySheetPhotos photos)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            if (photos.CoverImage != null)
            {
                col.Item().Element(c => ComposePhotos(c, photos));
            }

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
