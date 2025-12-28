using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Repositories;

public class RentInvoiceLineRepository : Repository<RentInvoiceLine>, IRentInvoiceLineRepository
{
    public RentInvoiceLineRepository(LocaGuestDbContext context) : base(context)
    {
    }

    public async Task<List<RentInvoiceLine>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RentInvoiceLine>()
            .Where(l => l.RentInvoiceId == invoiceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RentInvoiceLine?> GetByInvoiceTenantAsync(Guid invoiceId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RentInvoiceLine>()
            .FirstOrDefaultAsync(l => l.RentInvoiceId == invoiceId && l.TenantId == tenantId, cancellationToken);
    }
}
