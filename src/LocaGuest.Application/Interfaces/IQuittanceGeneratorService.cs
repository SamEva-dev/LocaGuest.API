namespace LocaGuest.Application.Interfaces;

public interface IQuittanceGeneratorService
{
    Task<byte[]> GenerateQuittancePdfAsync(
        string tenantName,
        string tenantEmail,
        string propertyName,
        string propertyAddress,
        string propertyCity,
        decimal amount,
        DateTime paymentDate,
        string month,
        string? reference,
        CancellationToken cancellationToken = default);
}
