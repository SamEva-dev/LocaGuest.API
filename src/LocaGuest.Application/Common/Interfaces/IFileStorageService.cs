namespace LocaGuest.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Sauvegarder un fichier et retourner le chemin relatif
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string subPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lire un fichier et retourner le contenu
    /// </summary>
    Task<byte[]> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Supprimer un fichier
    /// </summary>
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Vérifier si un fichier existe
    /// </summary>
    Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default);
}

public class UploadedFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Length { get; set; }
    public Stream Stream { get; set; } = Stream.Null;
}
