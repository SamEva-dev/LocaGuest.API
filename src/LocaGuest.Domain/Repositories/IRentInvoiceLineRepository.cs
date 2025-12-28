using LocaGuest.Domain.Aggregates.PaymentAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IRentInvoiceLineRepository : IRepository<RentInvoiceLine>
{
    Task<List<RentInvoiceLine>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<RentInvoiceLine?> GetByInvoiceTenantAsync(Guid invoiceId, Guid tenantId, CancellationToken cancellationToken = default);
}
