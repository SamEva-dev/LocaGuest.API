namespace LocaGuest.Application.DTOs.Documents;

using System.Text.Json.Serialization;

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
    public string OccupantName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class GenerateDocumentRequest
{
    public string TemplateType { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Guid? OccupantId { get; set; }
    public string? Notes { get; set; }
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? OccupantId { get; set; }
    public string? OccupantName { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UploadDocumentDto
{
    public required string FileName { get; set; }
    public required string Type { get; set; }
    public required string Category { get; set; }
    public required byte[] FileContent { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? OccupantId { get; set; }
    public Guid? PropertyId { get; set; }
}
