using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.RentabilityAggregate;

public class RentabilityScenario : AuditableEntity
{
    /// <summary>
    /// Auto-generated unique code (e.g., T0001-SCN0001)
    /// Format: {TenantCode}-SCN{Number}
    /// </summary>
    public string Code { get; private set; } = string.Empty;
    
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsBase { get; private set; }
    
    // Context
    public string PropertyType { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public decimal Surface { get; private set; }
    public string State { get; private set; } = string.Empty;
    public string Strategy { get; private set; } = string.Empty;
    public int Horizon { get; private set; }
    public string Objective { get; private set; } = string.Empty;
    public decimal PurchasePrice { get; private set; }
    public decimal NotaryFees { get; private set; }
    public decimal RenovationCost { get; private set; }
    public decimal? LandValue { get; private set; }
    public decimal? FurnitureCost { get; private set; }
    
    // Revenues
    public decimal MonthlyRent { get; private set; }
    public string Indexation { get; private set; } = string.Empty;
    public decimal IndexationRate { get; private set; }
    public decimal VacancyRate { get; private set; }
    public bool SeasonalityEnabled { get; private set; }
    public decimal? HighSeasonMultiplier { get; private set; }
    public decimal? ParkingRent { get; private set; }
    public decimal? StorageRent { get; private set; }
    public decimal? OtherRevenues { get; private set; }
    public bool GuaranteedRent { get; private set; }
    public decimal? RelocationIncrease { get; private set; }
    
    // Charges
    public decimal CondoFees { get; private set; }
    public decimal Insurance { get; private set; }
    public decimal PropertyTax { get; private set; }
    public decimal ManagementFees { get; private set; }
    public decimal MaintenanceRate { get; private set; }
    public decimal RecoverableCharges { get; private set; }
    public decimal ChargesIncrease { get; private set; }
    public string? PlannedCapexJson { get; private set; }
    
    // Financing
    public decimal LoanAmount { get; private set; }
    public string LoanType { get; private set; } = string.Empty;
    public decimal InterestRate { get; private set; }
    public int Duration { get; private set; }
    public decimal InsuranceRate { get; private set; }
    public int DeferredMonths { get; private set; }
    public string DeferredType { get; private set; } = string.Empty;
    public decimal EarlyRepaymentPenalty { get; private set; }
    public bool IncludeNotaryInLoan { get; private set; }
    public bool IncludeRenovationInLoan { get; private set; }
    
    // Tax
    public string TaxRegime { get; private set; } = string.Empty;
    public decimal MarginalTaxRate { get; private set; }
    public decimal SocialContributions { get; private set; }
    public int? DepreciationYears { get; private set; }
    public int? FurnitureDepreciationYears { get; private set; }
    public bool DeficitCarryForward { get; private set; }
    public bool CrlApplicable { get; private set; }
    
    // Exit
    public string ExitMethod { get; private set; } = string.Empty;
    public decimal? TargetCapRate { get; private set; }
    public decimal? AnnualAppreciation { get; private set; }
    public decimal? TargetPricePerSqm { get; private set; }
    public decimal SellingCosts { get; private set; }
    public decimal CapitalGainsTax { get; private set; }
    public int HoldYears { get; private set; }
    
    // Results (stored as JSON)
    public string? ResultsJson { get; private set; }
    
    // Organization and filtering
    public string? Tags { get; private set; }
    public string? Category { get; private set; }
    public bool IsFavorite { get; private set; }
    
    // Versioning & Sharing
    public int CurrentVersion { get; private set; } = 1;
    private readonly List<ScenarioVersion> _versions = new();
    public IReadOnlyCollection<ScenarioVersion> Versions => _versions.AsReadOnly();
    
    private readonly List<ScenarioShare> _shares = new();
    public IReadOnlyCollection<ScenarioShare> Shares => _shares.AsReadOnly();
    
    // Comments (not loaded by default, use explicit loading)
    private readonly List<ScenarioComment> _comments = new();
    public IReadOnlyCollection<ScenarioComment> Comments => _comments.AsReadOnly();
    
    private RentabilityScenario() { }
    
    public static RentabilityScenario Create(
        Guid userId,
        string name,
        bool isBase = false)
    {
        var now = DateTime.UtcNow;
        return new RentabilityScenario
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            IsBase = isBase,
            CreatedAt = now,
            LastModifiedAt = now
        };
    }
    
    /// <summary>
    /// Set the auto-generated code (called once after creation)
    /// Code is immutable after being set
    /// </summary>
    public void SetCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(Code))
            throw new InvalidOperationException("Code cannot be changed once set");
        
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));
        
        Code = code;
    }
    
    public void UpdateContext(
        string propertyType,
        string location,
        decimal surface,
        string state,
        string strategy,
        int horizon,
        string objective,
        decimal purchasePrice,
        decimal notaryFees,
        decimal renovationCost,
        decimal? landValue,
        decimal? furnitureCost)
    {
        PropertyType = propertyType;
        Location = location;
        Surface = surface;
        State = state;
        Strategy = strategy;
        Horizon = horizon;
        Objective = objective;
        PurchasePrice = purchasePrice;
        NotaryFees = notaryFees;
        RenovationCost = renovationCost;
        LandValue = landValue;
        FurnitureCost = furnitureCost;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateRevenues(
        decimal monthlyRent,
        string indexation,
        decimal indexationRate,
        decimal vacancyRate,
        bool seasonalityEnabled,
        decimal? highSeasonMultiplier,
        decimal? parkingRent,
        decimal? storageRent,
        decimal? otherRevenues,
        bool guaranteedRent,
        decimal? relocationIncrease)
    {
        MonthlyRent = monthlyRent;
        Indexation = indexation;
        IndexationRate = indexationRate;
        VacancyRate = vacancyRate;
        SeasonalityEnabled = seasonalityEnabled;
        HighSeasonMultiplier = highSeasonMultiplier;
        ParkingRent = parkingRent;
        StorageRent = storageRent;
        OtherRevenues = otherRevenues;
        GuaranteedRent = guaranteedRent;
        RelocationIncrease = relocationIncrease;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateCharges(
        decimal condoFees,
        decimal insurance,
        decimal propertyTax,
        decimal managementFees,
        decimal maintenanceRate,
        decimal recoverableCharges,
        decimal chargesIncrease,
        string? plannedCapexJson)
    {
        CondoFees = condoFees;
        Insurance = insurance;
        PropertyTax = propertyTax;
        ManagementFees = managementFees;
        MaintenanceRate = maintenanceRate;
        RecoverableCharges = recoverableCharges;
        ChargesIncrease = chargesIncrease;
        PlannedCapexJson = plannedCapexJson;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateFinancing(
        decimal loanAmount,
        string loanType,
        decimal interestRate,
        int duration,
        decimal insuranceRate,
        int deferredMonths,
        string deferredType,
        decimal earlyRepaymentPenalty,
        bool includeNotaryInLoan,
        bool includeRenovationInLoan)
    {
        LoanAmount = loanAmount;
        LoanType = loanType;
        InterestRate = interestRate;
        Duration = duration;
        InsuranceRate = insuranceRate;
        DeferredMonths = deferredMonths;
        DeferredType = deferredType;
        EarlyRepaymentPenalty = earlyRepaymentPenalty;
        IncludeNotaryInLoan = includeNotaryInLoan;
        IncludeRenovationInLoan = includeRenovationInLoan;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateTax(
        string taxRegime,
        decimal marginalTaxRate,
        decimal socialContributions,
        int? depreciationYears,
        int? furnitureDepreciationYears,
        bool deficitCarryForward,
        bool crlApplicable)
    {
        TaxRegime = taxRegime;
        MarginalTaxRate = marginalTaxRate;
        SocialContributions = socialContributions;
        DepreciationYears = depreciationYears;
        FurnitureDepreciationYears = furnitureDepreciationYears;
        DeficitCarryForward = deficitCarryForward;
        CrlApplicable = crlApplicable;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateExit(
        string exitMethod,
        decimal? targetCapRate,
        decimal? annualAppreciation,
        decimal? targetPricePerSqm,
        decimal sellingCosts,
        decimal capitalGainsTax,
        int holdYears)
    {
        ExitMethod = exitMethod;
        TargetCapRate = targetCapRate;
        AnnualAppreciation = annualAppreciation;
        TargetPricePerSqm = targetPricePerSqm;
        SellingCosts = sellingCosts;
        CapitalGainsTax = capitalGainsTax;
        HoldYears = holdYears;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateResults(string resultsJson)
    {
        ResultsJson = resultsJson;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateName(string name)
    {
        Name = name;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void SetAsBase()
    {
        IsBase = true;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UnsetAsBase()
    {
        IsBase = false;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    // Tags and categorization
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        
        var tags = GetTags();
        if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(tag);
            Tags = string.Join(",", tags);
            LastModifiedAt = DateTime.UtcNow;
        }
    }
    
    public void RemoveTag(string tag)
    {
        var tags = GetTags();
        tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        Tags = string.Join(",", tags);
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public List<string> GetTags()
    {
        if (string.IsNullOrWhiteSpace(Tags)) return new List<string>();
        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(t => t.Trim())
                   .ToList();
    }
    
    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void SetCategory(string category)
    {
        Category = category;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void CreateVersion(string changeDescription, string snapshotJson)
    {
        CurrentVersion++;
        var version = ScenarioVersion.Create(Id, CurrentVersion, changeDescription, snapshotJson);
        _versions.Add(version);
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void ShareWith(Guid userId, string permission, DateTime? expiresAt = null)
    {
        var existingShare = _shares.FirstOrDefault(s => s.SharedWithUserId == userId);
        if (existingShare != null)
        {
            _shares.Remove(existingShare);
        }
        
        var share = ScenarioShare.Create(Id, UserId, userId, permission, expiresAt);
        _shares.Add(share);
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void RevokeShare(Guid userId)
    {
        var share = _shares.FirstOrDefault(s => s.SharedWithUserId == userId);
        if (share != null)
        {
            _shares.Remove(share);
            LastModifiedAt = DateTime.UtcNow;
        }
    }
    
    public bool IsSharedWith(Guid userId)
    {
        return _shares.Any(s => s.SharedWithUserId == userId && s.CanView());
    }
    
    public bool CanUserEdit(Guid userId)
    {
        if (UserId == userId) return true;
        return _shares.Any(s => s.SharedWithUserId == userId && s.CanEdit());
    }
    
    // Comments management
    public void AddComment(Guid userId, string userName, string content, Guid? parentCommentId = null)
    {
        var comment = ScenarioComment.Create(Id, userId, userName, content, parentCommentId);
        _comments.Add(comment);
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateComment(Guid commentId, string newContent)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId);
        comment?.UpdateContent(newContent);
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void RemoveComment(Guid commentId)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId);
        if (comment != null)
        {
            // Remove all replies too
            var replies = _comments.Where(c => c.ParentCommentId == commentId).ToList();
            foreach (var reply in replies)
            {
                _comments.Remove(reply);
            }
            _comments.Remove(comment);
            LastModifiedAt = DateTime.UtcNow;
        }
    }
}
