using LocaGuest.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace LocaGuest.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<FileStorageService> _logger;
    private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".svg", ".webp" };
    private static readonly string[] AllowedImageContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/svg+xml",
        "image/webp"
    };

    public FileStorageService(IHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        // Use wwwroot directory for static files
        _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        _logger = logger;
        
        // Create wwwroot if it doesn't exist
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
            _logger.LogInformation("Created wwwroot directory at {RootPath}", _rootPath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string subPath, CancellationToken cancellationToken = default)
    {
        // Structure: uploads/{subPath}/{fileName}
        var relativePath = Path.Combine("uploads", subPath, fileName);
        var fullPath = Path.Combine(_rootPath, relativePath);
        
        // Créer le dossier si nécessaire
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Sauvegarder le fichier
        using (var fileStreamDest = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamDest, cancellationToken);
        }

        _logger.LogInformation("Fichier sauvegardé: {RelativePath}", relativePath);
        return relativePath;
    }

    public async Task<byte[]> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Fichier non trouvé: {relativePath}");
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Fichier supprimé: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public bool ValidateFile(string fileName, string contentType, long fileSize)
    {
        // Check file size
        if (fileSize > MaxFileSize)
        {
            _logger.LogWarning("File size exceeds limit: {FileSize} bytes (max: {MaxSize})", fileSize, MaxFileSize);
            return false;
        }

        if (fileSize <= 0)
        {
            _logger.LogWarning("File size is zero or negative: {FileSize}", fileSize);
            return false;
        }

        // Check content type
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedImageContentTypes.Contains(contentType.ToLower()))
        {
            _logger.LogWarning("Invalid content type: {ContentType}", contentType);
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(fileName)?.ToLower();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid file extension: {Extension}", extension);
            return false;
        }

        return true;
    }
}
