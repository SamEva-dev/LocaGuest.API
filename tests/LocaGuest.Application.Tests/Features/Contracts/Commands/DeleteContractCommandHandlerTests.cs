using LocaGuest.Application.Features.Contracts.Commands.DeleteContract;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class DeleteContractCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<ILogger<DeleteContractCommandHandler>> _loggerMock;
    private readonly DeleteContractCommandHandler _handler;

    public DeleteContractCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _loggerMock = new Mock<ILogger<DeleteContractCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Payments).Returns(_paymentRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Documents).Returns(_documentRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteContractCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _contractRepositoryMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Contract?)null);

        var result = await _handler.Handle(new DeleteContractCommand(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Contract not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenNotDraft_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        contract.MarkAsSigned();

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);

        var result = await _handler.Handle(new DeleteContractCommand(contractId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Only Draft contracts can be deleted", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenDraft_DeletesContract_AndReturnsCounts()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);

        var property = Property.Create(
            name: "Test",
            address: "Addr",
            city: "City",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.ColocationSolidaire,
            rent: 1000m,
            bedrooms: 3,
            bathrooms: 1,
            totalRooms: 2);
        property.SetOrganizationId(Guid.NewGuid());
        property.AddRoom("R1", 400m);
        property.AddRoom("R2", 400m);

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);

        _paymentRepositoryMock.Setup(x => x.GetByContractIdAsync(contract.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<LocaGuest.Domain.Aggregates.PaymentAggregate.Payment>());
        _documentRepositoryMock.Setup(x => x.GetByContractIdAsync(contract.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<LocaGuest.Domain.Aggregates.DocumentAggregate.Document>());

        var result = await _handler.Handle(new DeleteContractCommand(contractId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data!.DeletedPayments);
        Assert.Equal(0, result.Data!.DeletedDocuments);

        _contractRepositoryMock.Verify(x => x.Remove(contract), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
