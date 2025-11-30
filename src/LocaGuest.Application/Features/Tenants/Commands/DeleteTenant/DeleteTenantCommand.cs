using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Tenants.Commands.DeleteTenant;

public record DeleteTenantCommand : IRequest<Result<DeleteTenantResult>>
{
    public Guid TenantId { get; init; }
}

public record DeleteTenantResult
{
    public Guid Id { get; init; }
    public int DeletedContracts { get; init; }
    public int DeletedPayments { get; init; }
    public int DeletedDocuments { get; init; }
}
