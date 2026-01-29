using System.Globalization;
using System.Text;
using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Invoices.Queries.ExportInvoices;

public class ExportInvoicesQueryHandler : IRequestHandler<ExportInvoicesQuery, Result<ExportResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportInvoicesQueryHandler> _logger;

    public ExportInvoicesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<ExportInvoicesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ExportResultDto>> Handle(ExportInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Build query
            var query = _unitOfWork.RentInvoices.Query(asNoTracking: true);

            if (request.OccupantId.HasValue)
                query = query.Where(i => i.RenterOccupantId == request.OccupantId.Value);

            if (request.PropertyId.HasValue)
                query = query.Where(i => i.PropertyId == request.PropertyId.Value);

            if (request.StartDate.HasValue)
            {
                var startYear = request.StartDate.Value.Year;
                var startMonth = request.StartDate.Value.Month;
                query = query.Where(i => i.Year > startYear || (i.Year == startYear && i.Month >= startMonth));
            }

            if (request.EndDate.HasValue)
            {
                var endYear = request.EndDate.Value.Year;
                var endMonth = request.EndDate.Value.Month;
                query = query.Where(i => i.Year < endYear || (i.Year == endYear && i.Month <= endMonth));
            }

            var invoices = await query
                .OrderBy(i => i.Year)
                .ThenBy(i => i.Month)
                .ToListAsync(cancellationToken);

            if (request.Format.ToLower() == "csv")
            {
                var csv = GenerateCsv(invoices);
                return Result<ExportResultDto>.Success(new ExportResultDto(
                    Encoding.UTF8.GetBytes(csv),
                    $"factures_{DateTime.Now:yyyyMMdd}.csv",
                    "text/csv"
                ));
            }
            else
            {
                // Excel format not implemented yet - return CSV for now
                var csv = GenerateCsv(invoices);
                return Result<ExportResultDto>.Success(new ExportResultDto(
                    Encoding.UTF8.GetBytes(csv),
                    $"factures_{DateTime.Now:yyyyMMdd}.csv",
                    "text/csv"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices");
            return Result<ExportResultDto>.Failure<ExportResultDto>("Erreur lors de l'export des factures");
        }
    }

    private string GenerateCsv(List<Domain.Aggregates.PaymentAggregate.RentInvoice> invoices)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Date;Locataire ID;Bien ID;Contrat ID;Mois;Année;Montant;Date d'échéance;Date de paiement;Statut;Notes");

        // Data
        foreach (var invoice in invoices)
        {
            csv.AppendLine(string.Join(";",
                invoice.GeneratedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                invoice.RenterOccupantId,
                invoice.PropertyId,
                invoice.ContractId,
                invoice.Month,
                invoice.Year,
                invoice.Amount.ToString("F2", CultureInfo.InvariantCulture),
                invoice.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                invoice.PaidDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                invoice.Status.ToString(),
                EscapeCsvField(invoice.Notes ?? "")
            ));
        }

        return csv.ToString();
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains(";") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
