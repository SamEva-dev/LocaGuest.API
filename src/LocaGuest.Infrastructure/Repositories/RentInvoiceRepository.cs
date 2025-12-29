using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class RentInvoiceRepository : Repository<RentInvoice>, IRentInvoiceRepository
{
    public RentInvoiceRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<List<RentInvoice>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RentInvoice>()
            .Where(i => i.RenterTenantId == tenantId)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RentInvoice>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RentInvoice>()
            .Where(i => i.PropertyId == propertyId)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RentInvoice>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RentInvoice>()
            .Where(i => i.ContractId == contractId)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<RentInvoice?> GetByMonthYearAsync(Guid contractId, int month, int year, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RentInvoice>()
            .FirstOrDefaultAsync(i => 
                i.ContractId == contractId && 
                i.Month == month && 
                i.Year == year,
                cancellationToken);
    }

    public async Task<List<RentInvoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Set<RentInvoice>()
            .Where(i => i.DueDate < today && i.Status != InvoiceStatus.Paid)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RentInvoice>> GetCurrentMonthInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<RentInvoice>()
            .Where(i => i.Month == now.Month && i.Year == now.Year)
            .ToListAsync(cancellationToken);
    }
}
