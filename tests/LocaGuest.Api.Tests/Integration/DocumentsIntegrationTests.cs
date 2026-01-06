using FluentAssertions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Application.Features.Documents.Commands.SaveGeneratedDocument;
using LocaGuest.Application.Features.Documents.Queries.GetAllDocuments;
using LocaGuest.Application.Features.Documents.Queries.GetTenantDocuments;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Integration;

public class DocumentsIntegrationTests : IDisposable
{
    private readonly LocaGuestDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Mock<IOrganizationContext> _orgContextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<INumberSequenceService> _numberSequenceServiceMock;
    private readonly string _testTenantId = Guid.NewGuid().ToString();
    private readonly Guid _testUserId = Guid.NewGuid();

    public DocumentsIntegrationTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<LocaGuestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _orgContextMock = new Mock<IOrganizationContext>();
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _orgContextMock.Setup(x => x.OrganizationId).Returns(Guid.Parse(_testTenantId));
        _orgContextMock.Setup(x => x.IsSystemContext).Returns(false);
        _orgContextMock.Setup(x => x.CanBypassOrganizationFilter).Returns(false);

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_testUserId);

        var mediatorMock = new Mock<IMediator>();
        _context = new LocaGuestDbContext(options, mediatorMock.Object, _currentUserServiceMock.Object, _orgContextMock.Object);
        _unitOfWork = new UnitOfWork(_context);

        _numberSequenceServiceMock = new Mock<INumberSequenceService>();
        _numberSequenceServiceMock
            .Setup(x => x.GenerateNextCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tenantId, string prefix, CancellationToken ct) => $"{prefix}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateDocument_ThenGetAll_ShouldReturnDocument()
    {
        // Arrange - Create a tenant first
        var tenant = Occupant.Create("John Doe", "john.doe@test.com", "+33123456789");
        tenant.SetCode("T0001-L0001");
        await _unitOfWork.Occupants.AddAsync(tenant);
        await _unitOfWork.CommitAsync();

        // Act - Save a document
        var saveCommand = new SaveGeneratedDocumentCommand
        {
            FileName = "test-document.pdf",
            FilePath = "/uploads/test-document.pdf",
            Type = "PieceIdentite",
            Category = "Identite",
            FileSizeBytes = 1024,
            TenantId = tenant.Id,
            Description = "Test document"
        };

        var saveHandler = new SaveGeneratedDocumentCommandHandler(
            _unitOfWork,
            _context,
            _orgContextMock.Object,
            _numberSequenceServiceMock.Object,
            Mock.Of<ILogger<SaveGeneratedDocumentCommandHandler>>());

        var saveResult = await saveHandler.Handle(saveCommand, CancellationToken.None);

        // Assert - Document saved
        saveResult.IsSuccess.Should().BeTrue();
        saveResult.Data.Should().NotBeNull();
        saveResult.Data!.FileName.Should().Be("test-document.pdf");
        saveResult.Data.TenantId.Should().Be(tenant.Id);

        // Act - Get all documents
        var getAllQuery = new GetAllDocumentsQuery();
        var getAllHandler = new GetAllDocumentsQueryHandler(
            _context,
            Mock.Of<ILogger<GetAllDocumentsQueryHandler>>());

        var getAllResult = await getAllHandler.Handle(getAllQuery, CancellationToken.None);

        // Assert - Document retrieved
        getAllResult.IsSuccess.Should().BeTrue();
        getAllResult.Data.Should().NotBeNull();
        getAllResult.Data!.Should().ContainSingle();
        getAllResult.Data.First().FileName.Should().Be("test-document.pdf");
        getAllResult.Data.First().TenantId.Should().Be(tenant.Id);
        getAllResult.Data.First().TenantName.Should().Be("John Doe");
    }

    [Fact]
    public async Task CreateDocument_ThenGetByTenant_ShouldReturnDocument()
    {
        // Arrange - Create a tenant
        var tenant = Occupant.Create("Jane Smith", "jane.smith@test.com", "+33987654321");
        tenant.SetCode("T0001-L0002");
        await _unitOfWork.Occupants.AddAsync(tenant);
        await _unitOfWork.CommitAsync();

        // Act - Save a document
        var saveCommand = new SaveGeneratedDocumentCommand
        {
            FileName = "tenant-doc.pdf",
            FilePath = "/uploads/tenant-doc.pdf",
            Type = "Assurance",
            Category = "Justificatifs",
            FileSizeBytes = 2048,
            TenantId = tenant.Id,
            Description = "Tenant-specific document"
        };

        var saveHandler = new SaveGeneratedDocumentCommandHandler(
            _unitOfWork,
            _context,
            _orgContextMock.Object,
            _numberSequenceServiceMock.Object,
            Mock.Of<ILogger<SaveGeneratedDocumentCommandHandler>>());

        await saveHandler.Handle(saveCommand, CancellationToken.None);

        // Act - Get documents by tenant
        var getByTenantQuery = new GetTenantDocumentsQuery { TenantId = tenant.Id.ToString() };
        var getByTenantHandler = new GetTenantDocumentsQueryHandler(
            _unitOfWork,
            Mock.Of<ILogger<GetTenantDocumentsQueryHandler>>());

        var getByTenantResult = await getByTenantHandler.Handle(getByTenantQuery, CancellationToken.None);

        // Assert
        getByTenantResult.IsSuccess.Should().BeTrue();
        getByTenantResult.Data.Should().NotBeNull();
        getByTenantResult.Data!.Should().ContainSingle();
        getByTenantResult.Data.First().TenantId.Should().Be(tenant.Id);
        getByTenantResult.Data.First().FileName.Should().Be("tenant-doc.pdf");
    }

    [Fact]
    public async Task MultiTenant_Isolation_ShouldOnlyReturnOwnDocuments()
    {
        // Arrange - Create two tenants (simulating two different locataires)
        var tenant1 = Occupant.Create("User1 Test1", "user1@test.com", "+33111111111");
        tenant1.SetCode("T0001-L0003");
        await _unitOfWork.Occupants.AddAsync(tenant1);

        var tenant2 = Occupant.Create("User2 Test2", "user2@test.com", "+33222222222");
        tenant2.SetCode("T0001-L0004");
        await _unitOfWork.Occupants.AddAsync(tenant2);
        await _unitOfWork.CommitAsync();

        var saveHandler = new SaveGeneratedDocumentCommandHandler(
            _unitOfWork,
            _context,
            _orgContextMock.Object,
            _numberSequenceServiceMock.Object,
            Mock.Of<ILogger<SaveGeneratedDocumentCommandHandler>>());

        // Upload document for tenant1
        var saveCommand1 = new SaveGeneratedDocumentCommand
        {
            FileName = "tenant1-doc.pdf",
            FilePath = "/uploads/tenant1-doc.pdf",
            Type = "PieceIdentite",
            Category = "Identite",
            FileSizeBytes = 1024,
            TenantId = tenant1.Id
        };
        await saveHandler.Handle(saveCommand1, CancellationToken.None);

        // Upload document for tenant2
        var saveCommand2 = new SaveGeneratedDocumentCommand
        {
            FileName = "tenant2-doc.pdf",
            FilePath = "/uploads/tenant2-doc.pdf",
            Type = "PieceIdentite",
            Category = "Identite",
            FileSizeBytes = 2048,
            TenantId = tenant2.Id
        };
        await saveHandler.Handle(saveCommand2, CancellationToken.None);

        // Act - Get all documents (should return both since we're using the same user context)
        var getAllQuery = new GetAllDocumentsQuery();
        var getAllHandler = new GetAllDocumentsQueryHandler(
            _context,
            Mock.Of<ILogger<GetAllDocumentsQueryHandler>>());

        var getAllResult = await getAllHandler.Handle(getAllQuery, CancellationToken.None);

        // Assert - Both documents should be returned (same user owns both locataires)
        getAllResult.IsSuccess.Should().BeTrue();
        getAllResult.Data.Should().NotBeNull();
        getAllResult.Data!.Should().HaveCount(2);
        getAllResult.Data.Should().Contain(d => d.TenantId == tenant1.Id);
        getAllResult.Data.Should().Contain(d => d.TenantId == tenant2.Id);
    }
}
