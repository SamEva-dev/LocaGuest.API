using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Analytics.Queries.GetProfitabilityStats;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Infrastructure.Persistence;
using LocaGuest.Infrastructure.Repositories;

namespace LocaGuest.Application.Tests.Features.Analytics.Queries;

public class FakeOrganizationContext : IOrganizationContext
{
    public Guid? OrganizationId { get; }
    public bool IsAuthenticated { get; }
    public bool IsSystemContext { get; }
    public bool CanBypassOrganizationFilter { get; }

    public FakeOrganizationContext(Guid orgId, bool isAuthenticated = true)
    {
        OrganizationId = orgId;
        IsAuthenticated = isAuthenticated;
        IsSystemContext = false;
        CanBypassOrganizationFilter = false;
    }
}

public class GetProfitabilityStatsQueryHandlerTests
{
    private static LocaGuestDbContext CreateDb(Guid orgId)
    {
        var options = new DbContextOptionsBuilder<LocaGuestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mediator = new Mock<IMediator>();
        var orgCtx = new FakeOrganizationContext(orgId);

        return new LocaGuestDbContext(options, mediator.Object, currentUserService: null, organizationContext: orgCtx);
    }

    [Fact]
    public async Task Computes_MonthlyRevenue_Expenses_ProfitabilityRate_FromActiveContracts()
    {
        var orgId = Guid.NewGuid();
        await using var db = CreateDb(orgId);
        var uow = new UnitOfWork(db);

        var now = DateTime.UtcNow;

        var property = Property.Create(
            name: "P1",
            address: "Addr",
            city: "Nice",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.Complete,
            rent: 1000,
            bedrooms: 1,
            bathrooms: 1
        );

        property.SetOrganizationId(orgId);
        await db.AddAsync(property);
        await db.SaveChangesAsync();

        var c1 = Contract.Create(
            propertyId: property.Id,
            renterTenantId: Guid.NewGuid(),
            type: ContractType.Furnished,
            startDate: now.AddMonths(-2),
            endDate: now.AddMonths(10),
            rent: 1000,
            charges: 100
        );
        c1.SetOrganizationId(orgId);
        c1.MarkAsSigned(now.AddMonths(-2));
        c1.Activate();

        await db.AddAsync(c1);
        await db.SaveChangesAsync();

        var handler = new GetProfitabilityStatsQueryHandler(uow, NullLogger<GetProfitabilityStatsQueryHandler>.Instance);

        var result = await handler.Handle(new GetProfitabilityStatsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal(1100m, result.Data!.MonthlyRevenue);
        Assert.Equal(1100m * 0.15m, result.Data.MonthlyExpenses);
        Assert.Equal(result.Data.MonthlyRevenue - result.Data.MonthlyExpenses, result.Data.NetProfit);

        // Handler estimates property value from Property.Rent (excluding charges)
        var estimatedValue = 1000m * 12m * 20m;
        var expectedRate = (result.Data.NetProfit * 12m / estimatedValue) * 100m;
        Assert.Equal(Math.Round(expectedRate, 1), result.Data.ProfitabilityRate);
    }

    [Fact]
    public async Task LastMonthWindow_IncludesContractsStartingOnLastDayOfPreviousMonth()
    {
        var orgId = Guid.NewGuid();
        await using var db = CreateDb(orgId);
        var uow = new UnitOfWork(db);

        var now = DateTime.UtcNow;

        var property = Property.Create(
            name: "P1",
            address: "Addr",
            city: "Nice",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.Complete,
            rent: 1000,
            bedrooms: 1,
            bathrooms: 1
        );
        property.SetOrganizationId(orgId);
        await db.AddAsync(property);
        await db.SaveChangesAsync();

        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var lastMonthContract = Contract.Create(
            propertyId: property.Id,
            renterTenantId: Guid.NewGuid(),
            type: ContractType.Furnished,
            startDate: currentMonthStart.AddDays(-1).AddHours(12),
            endDate: currentMonthStart.AddMonths(6),
            rent: 900,
            charges: 0
        );
        lastMonthContract.SetOrganizationId(orgId);
        lastMonthContract.MarkAsSigned(lastMonthContract.StartDate);
        lastMonthContract.Activate();

        var currentMonthContract = Contract.Create(
            propertyId: property.Id,
            renterTenantId: Guid.NewGuid(),
            type: ContractType.Furnished,
            startDate: currentMonthStart.AddDays(1),
            endDate: currentMonthStart.AddMonths(6),
            rent: 1200,
            charges: 0
        );
        currentMonthContract.SetOrganizationId(orgId);
        currentMonthContract.MarkAsSigned(currentMonthContract.StartDate);
        currentMonthContract.Activate();

        await db.AddAsync(lastMonthContract);
        await db.AddAsync(currentMonthContract);
        await db.SaveChangesAsync();

        var handler = new GetProfitabilityStatsQueryHandler(uow, NullLogger<GetProfitabilityStatsQueryHandler>.Instance);
        var result = await handler.Handle(new GetProfitabilityStatsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.RevenueChangePercent != 0m);
    }
}
