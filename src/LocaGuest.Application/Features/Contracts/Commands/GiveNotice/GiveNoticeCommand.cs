using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.GiveNotice;

public record GiveNoticeCommand : IRequest<Result>
{
    public Guid ContractId { get; init; }
    public DateTime NoticeDate { get; init; }
    public DateTime NoticeEndDate { get; init; }
    public string Reason { get; init; } = string.Empty;
}
