using LocaGuest.Domain.Aggregates.PaymentAggregate;

namespace LocaGuest.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<List<Payment>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByMonthYearAsync(Guid contractId, int month, int year, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetLatePaymentsAsync(CancellationToken cancellationToken = default);
}
