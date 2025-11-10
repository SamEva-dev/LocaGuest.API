namespace LocaGuest.Application.DTOs.Documents;

public class DocumentStatsDto
{
    public int TotalDocuments { get; set; }
    public int ThisMonthDocuments { get; set; }
    public int ActiveTemplates { get; set; }
    public int TimeSavedHours { get; set; }
}

public class DocumentTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeneratedDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class GenerateDocumentRequest
{
    public string TemplateType { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Guid? TenantId { get; set; }
    public string? Notes { get; set; }
}
