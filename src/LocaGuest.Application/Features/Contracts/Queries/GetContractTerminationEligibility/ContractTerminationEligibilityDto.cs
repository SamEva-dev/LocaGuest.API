namespace LocaGuest.Application.Features.Contracts.Queries.GetContractTerminationEligibility;

public class ContractTerminationEligibilityDto
{
    public bool CanTerminate { get; set; }
    public bool HasInventoryEntry { get; set; }
    public bool HasInventoryExit { get; set; }
    public Guid? InventoryEntryId { get; set; }
    public Guid? InventoryExitId { get; set; }
    public bool PaymentsUpToDate { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int OverduePaymentsCount { get; set; }
    public string? BlockReason { get; set; }
}
