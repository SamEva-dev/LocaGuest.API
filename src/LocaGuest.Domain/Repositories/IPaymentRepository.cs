using LocaGuest.Domain.Aggregates.PaymentAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<List<Payment>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<List<Payment>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<List<Payment>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<Payment?> GetByMonthYearAsync(Guid contractId, Guid tenantId, int month, int year, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<List<Payment>> GetLatePaymentsAsync(CancellationToken cancellationToken = default, bool asNoTracking = false);
}
