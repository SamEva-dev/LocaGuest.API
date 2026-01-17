using FluentAssertions;
using LocaGuest.Application.Features.Contracts.Commands.TerminateContract;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class TerminateContractCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<ILogger<TerminateContractCommandHandler>> _loggerMock;
    private readonly TerminateContractCommandHandler _handler;

    public TerminateContractCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _loggerMock = new Mock<ILogger<TerminateContractCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);

        _handler = new TerminateContractCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _contractRepositoryMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Contract?)null);

        var result = await _handler.Handle(new TerminateContractCommand { ContractId = id, TerminationDate = DateTime.UtcNow, Reason = "x" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Contract not found");
    }

    [Fact]
    public async Task Handle_WhenNotActive_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        // Draft

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);

        var result = await _handler.Handle(new TerminateContractCommand { ContractId = contractId, TerminationDate = DateTime.UtcNow, Reason = "x" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Only Active contracts can be terminated");
    }

    [Fact]
    public async Task Handle_WhenReasonMissing_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        contract.MarkAsSigned();
        contract.Activate();

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);

        var result = await _handler.Handle(new TerminateContractCommand { ContractId = contractId, TerminationDate = DateTime.UtcNow, Reason = "  " }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("TERMINATION_REASON_REQUIRED");
    }
}
