using LocaGuest.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        // Utiliser le répertoire courant par défaut
        _rootPath = Directory.GetCurrentDirectory();
        _logger = logger;
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
}
