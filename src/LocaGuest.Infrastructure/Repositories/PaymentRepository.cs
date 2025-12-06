using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<List<Payment>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Payment>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.PropertyId == propertyId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Payment>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.ContractId == contractId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByMonthYearAsync(Guid contractId, int month, int year, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .FirstOrDefaultAsync(p => 
                p.ContractId == contractId && 
                p.Month == month && 
                p.Year == year,
                cancellationToken);
    }

    public async Task<List<Payment>> GetLatePaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.Status == PaymentStatus.Late || p.Status == PaymentStatus.PaidLate)
            .OrderByDescending(p => p.ExpectedDate)
            .ToListAsync(cancellationToken);
    }
}
