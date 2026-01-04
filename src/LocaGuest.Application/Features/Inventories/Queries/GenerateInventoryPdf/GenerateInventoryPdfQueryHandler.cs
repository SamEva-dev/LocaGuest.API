using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocaGuest.Application.Features.Inventories.Queries.GenerateInventoryPdf;

public class GenerateInventoryPdfQueryHandler : IRequestHandler<GenerateInventoryPdfQuery, Result<byte[]>>
{
    private readonly LocaGuest.Application.Common.Interfaces.ILocaGuestDbContext _context;
    private readonly ILogger<GenerateInventoryPdfQueryHandler> _logger;

    public GenerateInventoryPdfQueryHandler(
        LocaGuest.Application.Common.Interfaces.ILocaGuestDbContext context,
        ILogger<GenerateInventoryPdfQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(GenerateInventoryPdfQuery request, CancellationToken cancellationToken)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (request.InventoryType == "Entry")
            {
                var inventory = await _context.InventoryEntries.FindAsync(new object[] { request.InventoryId }, cancellationToken);
                if (inventory == null)
                    return Result.Failure<byte[]>("Inventory entry not found");

                var contract = await _context.Contracts.FindAsync(new object[] { inventory.ContractId }, cancellationToken);
                var property = contract != null ? await _context.Properties.FindAsync(new object[] { contract.PropertyId }, cancellationToken) : null;
                var tenant = contract != null ? await _context.Occupants.FindAsync(new object[] { contract.RenterTenantId }, cancellationToken) : null;

                var pdfBytes = GenerateEntryInventoryPdf(inventory, property?.Name, tenant?.FullName);
                return Result.Success(pdfBytes);
            }
            else
            {
                var inventory = await _context.InventoryExits.FindAsync(new object[] { request.InventoryId }, cancellationToken);
                if (inventory == null)
                    return Result.Failure<byte[]>("Inventory exit not found");

                var contract = await _context.Contracts.FindAsync(new object[] { inventory.ContractId }, cancellationToken);
                var property = contract != null ? await _context.Properties.FindAsync(new object[] { contract.PropertyId }, cancellationToken) : null;
                var tenant = contract != null ? await _context.Occupants.FindAsync(new object[] { contract.RenterTenantId }, cancellationToken) : null;
                var entryInventory = await _context.InventoryEntries.FindAsync(new object[] { inventory.InventoryEntryId }, cancellationToken);

                var pdfBytes = GenerateExitInventoryPdf(inventory, entryInventory, property?.Name, tenant?.FullName);
                return Result.Success(pdfBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory PDF {InventoryId}", request.InventoryId);
            return Result.Failure<byte[]>($"Error generating PDF: {ex.Message}");
        }
    }

    private byte[] GenerateEntryInventoryPdf(
        Domain.Aggregates.InventoryAggregate.InventoryEntry inventory,
        string? propertyName,
        string? tenantName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("ÉTAT DES LIEUX D'ENTRÉE").FontSize(20).Bold().AlignCenter();
                    column.Item().PaddingTop(10).Text($"Bien: {propertyName ?? "N/A"}").FontSize(12);
                    column.Item().Text($"Locataire: {tenantName ?? "N/A"}").FontSize(12);
                    column.Item().Text($"Date: {inventory.InspectionDate:dd/MM/yyyy}").FontSize(12);
                    column.Item().Text($"Agent: {inventory.AgentName}").FontSize(12);
                    column.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    column.Item().Text("Détail des éléments inspectés").FontSize(14).Bold();
                    column.Item().PaddingTop(10);

                    var itemsByRoom = inventory.Items.GroupBy(i => i.RoomName);

                    foreach (var roomGroup in itemsByRoom)
                    {
                        column.Item().PaddingTop(15).Text(roomGroup.Key).FontSize(13).Bold();
                        
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Élément").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("État").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Remarques").Bold();
                            });

                            foreach (var item in roomGroup)
                            {
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(item.ElementName);
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(GetConditionLabel(item.Condition.ToString()));
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(item.Comment ?? "-");
                            }
                        });
                    }

                    if (!string.IsNullOrEmpty(inventory.GeneralObservations))
                    {
                        column.Item().PaddingTop(20).Text("Observations générales").FontSize(13).Bold();
                        column.Item().PaddingTop(5).Text(inventory.GeneralObservations);
                    }
                });

                page.Footer().AlignCenter().Column(column =>
                {
                    column.Item().PaddingTop(20).LineHorizontal(1);
                    column.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Signature du locataire").FontSize(10);
                            c.Item().PaddingTop(30).LineHorizontal(0.5f);
                        });
                        row.Spacing(50);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Signature de l'agent").FontSize(10);
                            c.Item().PaddingTop(30).LineHorizontal(0.5f);
                        });
                    });
                    column.Item().PaddingTop(10).Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private byte[] GenerateExitInventoryPdf(
        Domain.Aggregates.InventoryAggregate.InventoryExit inventory,
        Domain.Aggregates.InventoryAggregate.InventoryEntry? entryInventory,
        string? propertyName,
        string? tenantName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("ÉTAT DES LIEUX DE SORTIE").FontSize(20).Bold().AlignCenter();
                    column.Item().PaddingTop(10).Text($"Bien: {propertyName ?? "N/A"}").FontSize(12);
                    column.Item().Text($"Locataire: {tenantName ?? "N/A"}").FontSize(12);
                    column.Item().Text($"Date: {inventory.InspectionDate:dd/MM/yyyy}").FontSize(12);
                    column.Item().Text($"Agent: {inventory.AgentName}").FontSize(12);
                    column.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    column.Item().Text("Comparaison Entrée / Sortie").FontSize(14).Bold();
                    column.Item().PaddingTop(10);

                    var comparisonsByRoom = inventory.Comparisons.GroupBy(c => c.RoomName);

                    foreach (var roomGroup in comparisonsByRoom)
                    {
                        column.Item().PaddingTop(15).Text(roomGroup.Key).FontSize(13).Bold();
                        
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Élément").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("État Entrée").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("État Sortie").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Dégradation").Bold();
                            });

                            foreach (var item in roomGroup)
                            {
                                var bgColor = item.HasDegradation ? Colors.Red.Lighten4 : Colors.White;
                                table.Cell().Background(bgColor).BorderBottom(0.5f).Padding(5).Text(item.ElementName);
                                table.Cell().Background(bgColor).BorderBottom(0.5f).Padding(5).Text(GetConditionLabel(item.EntryCondition.ToString()));
                                table.Cell().Background(bgColor).BorderBottom(0.5f).Padding(5).Text(GetConditionLabel(item.ExitCondition.ToString()));
                                table.Cell().Background(bgColor).BorderBottom(0.5f).Padding(5).Text(item.HasDegradation ? "Oui ⚠️" : "Non");
                            }
                        });
                    }

                    if (inventory.Degradations.Any())
                    {
                        column.Item().PaddingTop(20).Text("Dégradations constatées").FontSize(13).Bold();
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Pièce").Bold();
                                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Description").Bold();
                                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Coût").Bold();
                                header.Cell().Background(Colors.Red.Lighten3).Padding(5).Text("Imputable").Bold();
                            });

                            foreach (var deg in inventory.Degradations)
                            {
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(deg.RoomName);
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(deg.Description);
                                table.Cell().BorderBottom(0.5f).Padding(5).Text($"{deg.EstimatedCost:C}");
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(deg.IsImputedToTenant ? "Locataire" : "Propriétaire");
                            }
                        });

                        var totalTenant = inventory.Degradations.Where(d => d.IsImputedToTenant).Sum(d => d.EstimatedCost);
                        var totalOwner = inventory.Degradations.Where(d => !d.IsImputedToTenant).Sum(d => d.EstimatedCost);

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text($"Total imputable au locataire: {totalTenant:C}").Bold();
                            row.RelativeItem().Text($"Total pris en charge par le propriétaire: {totalOwner:C}");
                        });
                    }

                    if (!string.IsNullOrEmpty(inventory.GeneralObservations))
                    {
                        column.Item().PaddingTop(20).Text("Observations générales").FontSize(13).Bold();
                        column.Item().PaddingTop(5).Text(inventory.GeneralObservations);
                    }
                });

                page.Footer().AlignCenter().Column(column =>
                {
                    column.Item().PaddingTop(20).LineHorizontal(1);
                    column.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Signature du locataire").FontSize(10);
                            c.Item().PaddingTop(30).LineHorizontal(0.5f);
                        });
                        row.Spacing(50);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Signature de l'agent").FontSize(10);
                            c.Item().PaddingTop(30).LineHorizontal(0.5f);
                        });
                    });
                    column.Item().PaddingTop(10).Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private string GetConditionLabel(string condition)
    {
        return condition switch
        {
            "New" => "Neuf ⭐",
            "Good" => "Bon état ✓",
            "Fair" => "État moyen ○",
            "Poor" => "Mauvais état ⚠",
            "Damaged" => "Endommagé ✗",
            _ => condition
        };
    }
}
