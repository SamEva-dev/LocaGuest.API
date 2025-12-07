namespace LocaGuest.Application.DTOs.Properties;

public class PropertyImageDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = "other";
    public string MimeType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UploadImagesRequest
{
    public Guid PropertyId { get; set; }
    public string Category { get; set; } = "other";
}

public class UploadImagesResponse
{
    public List<PropertyImageDto> Images { get; set; } = new();
    public int Count { get; set; }
}
