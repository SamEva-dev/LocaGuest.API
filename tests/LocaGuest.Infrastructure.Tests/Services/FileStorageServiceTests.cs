using FluentAssertions;
using LocaGuest.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Infrastructure.Tests.Services;

public class FileStorageServiceTests : IDisposable
{
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock;
    private readonly Mock<ILogger<FileStorageService>> _loggerMock;
    private readonly FileStorageService _service;
    private readonly string _testDirectory;

    public FileStorageServiceTests()
    {
        // Setup test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileStorageTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(x => x.ContentRootPath).Returns(_testDirectory);

        _loggerMock = new Mock<ILogger<FileStorageService>>();

        _service = new FileStorageService(_hostEnvironmentMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_WithValidFile_SavesSuccessfully()
    {
        // Arrange
        var fileName = "test.png";
        var contentType = "image/png";
        var subPath = "logos";
        var content = "Test file content"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var result = await _service.SaveFileAsync(stream, fileName, contentType, subPath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(subPath);
        result.Should().EndWith(".png");

        var fullPath = Path.Combine(_testDirectory, "wwwroot", result);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var subPath = "newsubfolder";
        using var stream = new MemoryStream("content"u8.ToArray());

        // Act
        var result = await _service.SaveFileAsync(stream, fileName, contentType, subPath);

        // Assert
        var directory = Path.Combine(_testDirectory, "wwwroot", "uploads", subPath);
        Directory.Exists(directory).Should().BeTrue();
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg", 1024, true)]
    [InlineData("image.png", "image/png", 2 * 1024 * 1024, true)]  // 2MB exact
    [InlineData("image.svg", "image/svg+xml", 1024, true)]
    [InlineData("image.webp", "image/webp", 1024, true)]
    [InlineData("image.jpg", "image/jpeg", 3 * 1024 * 1024, false)] // 3MB - too large
    [InlineData("image.jpg", "image/jpeg", 0, false)]  // 0 bytes
    [InlineData("image.pdf", "application/pdf", 1024, false)]  // Invalid type
    [InlineData("image.txt", "text/plain", 1024, false)]  // Invalid extension
    public void ValidateFile_WithVariousInputs_ReturnsExpectedResult(
        string fileName, 
        string contentType, 
        long fileSize, 
        bool expectedResult)
    {
        // Act
        var result = _service.ValidateFile(fileName, contentType, fileSize);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_DeletesSuccessfully()
    {
        // Arrange
        var fileName = "test-delete.png";
        var contentType = "image/png";
        var subPath = "logos";
        using var stream = new MemoryStream("content"u8.ToArray());
        
        var filePath = await _service.SaveFileAsync(stream, fileName, contentType, subPath);
        var fullPath = Path.Combine(_testDirectory, "wwwroot", filePath);
        
        // Verify file exists
        File.Exists(fullPath).Should().BeTrue();

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Arrange
        var nonExistentPath = "uploads/logos/nonexistent.png";

        // Act & Assert
        await _service.DeleteFileAsync(nonExistentPath); // Should not throw
    }

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var fileName = "test-exists.png";
        var contentType = "image/png";
        var subPath = "logos";
        using var stream = new MemoryStream("content"u8.ToArray());
        
        var filePath = await _service.SaveFileAsync(stream, fileName, contentType, subPath);

        // Act
        var result = await _service.FileExistsAsync(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = "uploads/logos/nonexistent.png";

        // Act
        var result = await _service.FileExistsAsync(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReadFileAsync_WithExistingFile_ReturnsContent()
    {
        // Arrange
        var fileName = "test-read.png";
        var contentType = "image/png";
        var subPath = "logos";
        var originalContent = "Test file content for reading"u8.ToArray();
        using var stream = new MemoryStream(originalContent);
        
        var filePath = await _service.SaveFileAsync(stream, fileName, contentType, subPath);

        // Act
        var result = await _service.ReadFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(originalContent);
    }

    [Fact]
    public async Task ReadFileAsync_WithNonExistentFile_ThrowsException()
    {
        // Arrange
        var nonExistentPath = "uploads/logos/nonexistent.png";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.ReadFileAsync(nonExistentPath));
    }
}
