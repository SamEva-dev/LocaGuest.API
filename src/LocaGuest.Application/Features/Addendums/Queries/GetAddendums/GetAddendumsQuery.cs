using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Addendums;
using MediatR;

namespace LocaGuest.Application.Features.Addendums.Queries.GetAddendums;

public record GetAddendumsQuery : IRequest<Result<PagedResult<AddendumDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    public Guid? ContractId { get; init; }
    public string? Type { get; init; }
    public string? SignatureStatus { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
