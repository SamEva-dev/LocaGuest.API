namespace LocaGuest.Application.Interfaces;

public interface IInvoicePdfGeneratorService
{
    Task<byte[]> GenerateInvoicePdfAsync(
        string invoiceNumber,
        string tenantName,
        string? tenantEmail,
        string propertyName,
        string? propertyAddress,
        string? propertyCity,
        int month,
        int year,
        DateTime dueDate,
        decimal totalAmount,
        IReadOnlyCollection<(string label, decimal amount)> lines,
        CancellationToken cancellationToken = default);
}
