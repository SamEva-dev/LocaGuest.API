using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Invoices.Commands.GenerateMonthlyInvoices;

public record GenerateMonthlyInvoicesCommand(
    int Month,
    int Year
) : IRequest<Result<GenerateInvoicesResultDto>>;

public record GenerateInvoicesResultDto(
    int GeneratedCount,
    int SkippedCount,
    List<string> Errors
);
