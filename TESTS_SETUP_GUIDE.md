# 🧪 Guide de Configuration des Tests Unitaires - LocaGuest.API

**Date:** 15 novembre 2025  
**Statut:** ✅ **EN COURS**

---

## 📋 Structure des Projets de Test

### Arborescence Créée

```
LocaGuest.API/
├── src/
│   ├── LocaGuest.Api/
│   ├── LocaGuest.Application/
│   ├── LocaGuest.Domain/
│   └── LocaGuest.Infrastructure/
└── tests/
    ├── LocaGuest.Api.Tests/              ✅ Controllers Tests
    ├── LocaGuest.Application.Tests/       ✅ Handlers & Services Tests
    ├── LocaGuest.Domain.Tests/            ✅ Entities & Value Objects Tests
    └── LocaGuest.Infrastructure.Tests/    ✅ Repositories & DbContext Tests
```

---

## 📦 Packages NuGet Installés

### LocaGuest.Api.Tests

```xml
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="FluentAssertions" Version="8.8.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
<PackageReference Include="AutoFixture.AutoMoq" Version="4.18.1" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
```

---

## 🏗️ Patterns de Test Implémentés

### 1. Builder Pattern

**Localisation:** `tests/LocaGuest.Api.Tests/Builders/`

#### PropertyDtoBuilder
```csharp
public class PropertyDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Property";
    // ... autres champs
    
    public PropertyDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public PropertyDto Build() => new PropertyDto { ... };
    
    public static PropertyDtoBuilder AProperty() => new();
}
```

**Usage:**
```csharp
var property = PropertyDtoBuilder.AProperty()
    .WithName("My Apartment")
    .WithCity("Paris")
    .Build();
```

#### TenantDtoBuilder
```csharp
public class TenantDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _firstName = "John";
    private string _lastName = "Doe";
    // ... autres champs
    
    public TenantDto Build() => new TenantDto { ... };
    
    public static TenantDtoBuilder ATenant() => new();
}
```

---

### 2. Base Test Fixture avec AutoFixture

**Localisation:** `tests/LocaGuest.Api.Tests/Fixtures/BaseTestFixture.cs`

```csharp
public class BaseTestFixture
{
    public IFixture Fixture { get; }

    public BaseTestFixture()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Éviter les références circulaires
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        CustomizeFixture();
    }

    protected virtual void CustomizeFixture()
    {
        // À surcharger dans les classes dérivées
    }
}
```

---

### 3. Controller Tests

**Localisation:** `tests/LocaGuest.Api.Tests/Controllers/`

#### Structure d'un Test de Controller

```csharp
public class PropertiesControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<PropertiesController>> _loggerMock;
    private readonly PropertiesController _controller;

    public PropertiesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<PropertiesController>>();
        _controller = new PropertiesController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetProperties_WithSuccessfulQuery_ReturnsOkWithProperties()
    {
        // Arrange
        var properties = new List<PropertyDto> { ... };
        var pagedResult = new PagedResult<PropertyDto> { Items = properties };
        var result = Result.Success(pagedResult);
        var query = new GetPropertiesQuery();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPropertiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetProperties(query);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## 🎯 Conventions de Nommage

### Méthodes de Test
```
[MethodName]_[Scenario]_[ExpectedBehavior]
```

**Exemples:**
- `GetProperties_WithSuccessfulQuery_ReturnsOkWithProperties`
- `CreateProperty_WithInvalidCommand_ReturnsBadRequest`
- `GetProperty_WithValidId_ReturnsOkWithProperty`

### Classes de Test
```
[ClassToTest]Tests
```

**Exemples:**
- `PropertiesControllerTests`
- `TenantsControllerTests`
- `CreatePropertyCommandHandlerTests`

---

## 🧪 Types de Tests à Implémenter

### Controllers (API Layer)
✅ **Status Code Tests**
- 200 OK
- 201 Created
- 400 Bad Request
- 404 Not Found
- 500 Internal Server Error

✅ **Validation Tests**
- Required fields
- Format validation
- Business rules

✅ **Authorization Tests**
- Authenticated users
- Tenant isolation

### Handlers (Application Layer)
- Command validation
- Query execution
- Business logic
- Error handling
- Transaction management

### Repositories (Infrastructure Layer)
- CRUD operations
- Filtering & Pagination
- Multi-tenant isolation
- Concurrency handling

### Domain Entities
- Entity creation
- Value objects
- Domain events
- Business rules validation

---

## 🔧 Commandes Utiles

### Exécuter tous les tests
```bash
dotnet test
```

### Exécuter les tests d'un projet spécifique
```bash
dotnet test tests/LocaGuest.Api.Tests/
```

### Exécuter avec couverture de code
```bash
dotnet test /p:CollectCoverage=true
```

### Exécuter un test spécifique
```bash
dotnet test --filter "FullyQualifiedName~PropertiesControllerTests"
```

---

## 📊 Couverture de Code Cible

| Couche | Cible | Actuel |
|--------|-------|--------|
| Controllers | 80%+ | 🚧 En cours |
| Handlers | 90%+ | ⏳ À faire |
| Repositories | 85%+ | ⏳ À faire |
| Domain Entities | 95%+ | ⏳ À faire |

---

## ✅ Controllers Testés

### Complétés
- ⏳ PropertiesController (en cours)
- ⏳ TenantsController (en cours)

### À Faire
- ⏳ ContractsController
- ⏳ DashboardController
- ⏳ DocumentsController
- ⏳ AnalyticsController
- ⏳ SettingsController
- ⏳ SubscriptionsController
- ⏳ CheckoutController
- ⏳ StripeWebhookController
- ⏳ TrackingController
- ⏳ RentabilityScenariosController

---

## 🎯 Prochaines Étapes

1. ✅ Créer la structure des projets de test
2. ✅ Ajouter les packages NuGet
3. ✅ Créer les Builders et Fixtures
4. 🚧 Implémenter les tests des Controllers
5. ⏳ Implémenter les tests des Handlers
6. ⏳ Implémenter les tests des Repositories
7. ⏳ Implémenter les tests des Domain Entities
8. ⏳ Configurer la couverture de code
9. ⏳ Intégrer CI/CD avec tests automatiques

---

## 📚 Ressources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [AutoFixture Documentation](https://github.com/AutoFixture/AutoFixture)

---

**🎯 Objectif:** Atteindre 85%+ de couverture de code avec des tests maintenables et fiables
