using LocaGuest.Domain.Aggregates.PaymentAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IRentInvoiceRepository : IRepository<RentInvoice>
{
    Task<List<RentInvoice>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<RentInvoice>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<List<RentInvoice>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<RentInvoice?> GetByMonthYearAsync(Guid contractId, int month, int year, CancellationToken cancellationToken = default);
    Task<List<RentInvoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default);
    Task<List<RentInvoice>> GetCurrentMonthInvoicesAsync(CancellationToken cancellationToken = default);
}
