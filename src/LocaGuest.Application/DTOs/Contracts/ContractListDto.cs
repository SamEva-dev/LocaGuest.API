namespace LocaGuest.Application.DTOs.Contracts;

public class ContractListDto
{
    public Guid Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyCode { get; set; } = string.Empty;
    public Guid OccupantId { get; set; }
    public string OccupantName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Rent { get; set; }
    public decimal Charges { get; set; }
    public decimal Deposit { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? RoomId { get; set; }
    public bool IsConflict { get; set; }
}
