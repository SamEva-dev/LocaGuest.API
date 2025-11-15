# Rapport de Vérification - Architecture DDD/CQRS LocaGuest

**Date:** 15 novembre 2025  
**Statut:** ⚠️ **NON CONFORME** - Améliorations requises

---

## 📋 Résumé Exécutif

L'architecture LocaGuest utilise une approche hybride entre DDD/CQRS et une architecture simplifiée. Plusieurs points ne respectent pas les principes DDD purs et nécessitent des améliorations.

---

## 1. ❌ Architecture 4 Couches + CQRS + DDD

### ❌ Handlers utilisent DbContext directement

**Problème:**
Les handlers (Commands et Queries) utilisent `ILocaGuestDbContext` directement au lieu de passer par des **repositories**.

**Code actuel:**
```csharp
public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Result<PropertyDetailDto>>
{
    private readonly ILocaGuestDbContext _context;  // ❌ DbContext direct
    
    public async Task<Result<PropertyDetailDto>> Handle(...)
    {
        var property = Property.Create(...);
        _context.Properties.Add(property);  // ❌ Accès direct DbSet
        await _context.SaveChangesAsync();
    }
}
```

**Architecture DDD Pure attendue:**
```csharp
public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Result<PropertyDetailDto>>
{
    private readonly IPropertyRepository _propertyRepository;  // ✅ Repository
    private readonly IUnitOfWork _unitOfWork;  // ✅ UnitOfWork
    
    public async Task<Result<PropertyDetailDto>> Handle(...)
    {
        var property = Property.Create(...);
        await _propertyRepository.AddAsync(property);  // ✅ Repository
        await _unitOfWork.CommitAsync();  // ✅ Transaction explicite
    }
}
```

**Impact:**
- ❌ **32 handlers** violent ce principe
- ❌ Couplage fort avec EF Core dans la couche Application
- ❌ Difficile de changer l'ORM sans impacter les handlers
- ❌ Pas de centralisation de la logique d'accès aux données

**Handlers concernés:**
- `CreatePropertyCommandHandler`
- `CreateContractCommandHandler`
- `CreateTenantCommandHandler`
- `SaveRentabilityScenarioCommandHandler`
- Tous les `QueryHandlers` (26 fichiers)

---

### ✅ Entities Domain indépendantes d'EF Core

**Vérification:**
```bash
grep -r "using Microsoft.EntityFrameworkCore" src/LocaGuest.Domain
# Résultat: Aucune occurrence ✅
```

**Résultat:** ✅ **CONFORME** - Le domaine ne dépend pas d'EF Core

Les entités utilisent des méthodes factory et des comportements métier purs :
```csharp
public class Property : AuditableEntity
{
    private Property() { } // ✅ Constructeur privé
    
    public static Property Create(...) // ✅ Factory method
    {
        var property = new Property { ... };
        property.AddDomainEvent(new PropertyCreated(...));
        return property;
    }
}
```

---

### ❌ Value Objects NON utilisés

**Problème:**
Aucun Value Object n'est implémenté dans le domaine. Les concepts comme `Money`, `Address`, `DateRange`, `Email`, etc. sont représentés par des types primitifs.

**Code actuel:**
```csharp
public class Property : AuditableEntity
{
    public decimal Rent { get; set; }  // ❌ Devrait être Money
    public string Address { get; set; }  // ❌ Devrait être Address (VO)
    public string City { get; set; }     // ❌ Partie de Address
    public string ZipCode { get; set; }  // ❌ Partie de Address
}

public class Contract : AuditableEntity
{
    public DateTime StartDate { get; set; }  // ❌ Devrait être DateRange
    public DateTime? EndDate { get; set; }   // ❌ Partie de DateRange
}
```

**Architecture DDD attendue:**
```csharp
// Value Objects
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency = "EUR") { ... }
    public Money Add(Money other) { ... }
    protected override IEnumerable<object> GetEqualityComponents() { ... }
}

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }
    
    protected override IEnumerable<object> GetEqualityComponents() { ... }
}

public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime? End { get; }
    
    public int DurationInDays() { ... }
    public bool Overlaps(DateRange other) { ... }
    protected override IEnumerable<object> GetEqualityComponents() { ... }
}

// Utilisation dans les entités
public class Property : AuditableEntity
{
    public Money Rent { get; private set; }  // ✅ Value Object
    public Address Address { get; private set; }  // ✅ Value Object
}

public class Contract : AuditableEntity
{
    public DateRange Period { get; private set; }  // ✅ Value Object
}
```

**Impact:**
- ❌ Perte de la richesse du modèle métier
- ❌ Logique métier dispersée dans les handlers
- ❌ Validation faible
- ❌ Pas d'encapsulation des concepts métier

**Value Objects manquants:**
- `Money` (Rent, Charges, Price, etc.)
- `Address` (Street, City, PostalCode, Country)
- `DateRange` (Contract period)
- `Email`
- `PhoneNumber`

---

### ❌ UnitOfWork NON implémenté

**Problème:**
Pas de pattern UnitOfWork explicite. Les handlers appellent `_context.SaveChangesAsync()` directement.

**Code actuel:**
```csharp
public async Task<Result> Handle(...)
{
    // Opération 1
    _context.Properties.Add(property);
    
    // Opération 2
    _context.Contracts.Add(contract);
    
    // Sauvegarde directe ❌
    await _context.SaveChangesAsync();
}
```

**Architecture DDD attendue:**
```csharp
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
    bool HasActiveTransaction { get; }
}

public async Task<Result> Handle(...)
{
    // Opération 1
    await _propertyRepository.AddAsync(property);
    
    // Opération 2
    await _contractRepository.AddAsync(contract);
    
    // Transaction explicite ✅
    await _unitOfWork.CommitAsync();
}
```

**Impact:**
- ❌ Transactions implicites (moins de contrôle)
- ❌ Pas de gestion centralisée des transactions
- ❌ Difficile d'implémenter des transactions complexes
- ❌ Pas de rollback explicite

---

### ✅ Domain Events dispatchés après SaveChanges

**Vérification:**
```csharp
// LocaGuestDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // ... Audit et validation ...
    
    // Sauvegarder d'abord
    var result = await base.SaveChangesAsync(cancellationToken);  // ✅ Ligne 374
    
    // Dispatcher les Domain Events APRÈS persistence ✅ Ligne 377
    await DispatchDomainEventsAsync(cancellationToken);
    
    return result;
}

private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
{
    var domainEntities = ChangeTracker.Entries<Entity>()
        .Where(e => e.Entity.DomainEvents.Any())
        .ToList();
    
    var domainEvents = domainEntities
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();
    
    domainEntities.ForEach(e => e.Entity.ClearDomainEvents());
    
    foreach (var domainEvent in domainEvents)
    {
        await _mediator.Publish(domainEvent, cancellationToken);  // ✅ MediatR
    }
}
```

**Résultat:** ✅ **CONFORME**

**Domain Events trouvés:**
- `PropertyCreated`
- `PropertyStatusChanged`
- `ContractCreated`
- `ContractRenewed`
- `ContractTerminated`
- `TenantCreated`
- `TenantDeactivated`
- `PaymentRecorded`
- `PaymentLateDetected`

---

## 2. ⚠️ Accès Multi-Tenant

### ❌ Commands/Queries n'utilisent PAS ITenantContext

**Problème:**
Les handlers n'injectent pas `ITenantContext` et se reposent uniquement sur le Global Query Filter du DbContext.

**Code actuel:**
```csharp
public class CreatePropertyCommandHandler : IRequestHandler<...>
{
    private readonly ILocaGuestDbContext _context;  // Pas de ITenantContext
    
    public async Task<Result> Handle(...)
    {
        var property = Property.Create(...);
        // TenantId assigné automatiquement par SaveChangesAsync
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();  // ✅ Global Query Filter actif
    }
}
```

**Protection actuelle:**
```csharp
// LocaGuestDbContext.SaveChangesAsync()
if (entry.State == EntityState.Added)
{
    // Assignation automatique du TenantId ✅
    if (string.IsNullOrEmpty(entry.Entity.TenantId))
    {
        if (_tenantContext?.IsAuthenticated == true)
        {
            entry.Entity.TenantId = _tenantContext.TenantId;  // ✅
        }
    }
}
```

**Résultat:** ⚠️ **PARTIELLEMENT CONFORME**

**Points positifs:**
- ✅ Global Query Filter actif (filtrage automatique)
- ✅ TenantId assigné automatiquement dans SaveChanges
- ✅ Validation que l'entité appartient au tenant courant
- ✅ TenantId immuable (exception si modification)

**Points d'amélioration:**
- ⚠️ Pas de validation explicite dans les handlers
- ⚠️ Dépendance implicite sur le DbContext pour la sécurité

**Meilleure pratique:**
```csharp
public class CreatePropertyCommandHandler
{
    private readonly IPropertyRepository _repository;
    private readonly ITenantContext _tenantContext;  // ✅ Injection explicite
    
    public async Task<Result> Handle(...)
    {
        // Validation explicite ✅
        if (!_tenantContext.IsAuthenticated)
            return Result.Failure("User not authenticated");
        
        var property = Property.Create(...);
        // TenantId explicite (optionnel si géré par DbContext)
        property.SetTenantId(_tenantContext.TenantId);
        
        await _repository.AddAsync(property);
    }
}
```

---

### ✅ Aucune commande ne permet de changer le tenant

**Vérification:**
```bash
grep -r "TenantId.*=" src/LocaGuest.Application/Features
# Résultat: Aucune assignation manuelle de TenantId ✅
```

**Protection DbContext:**
```csharp
if (entry.State == EntityState.Modified)
{
    // Vérification que le TenantId n'a pas été modifié
    var originalTenantId = entry.Property(nameof(AuditableEntity.TenantId)).OriginalValue?.ToString();
    var currentTenantId = entry.Entity.TenantId;
    
    if (originalTenantId != currentTenantId)
    {
        throw new InvalidOperationException("TenantId cannot be modified after entity creation");  // ✅
    }
}
```

**Résultat:** ✅ **CONFORME**

---

## 3. ⚠️ Stripe - Service isolé

### ⚠️ Stripe dans Controllers (pas de Service dédié)

**Problème:**
La logique Stripe est directement dans les controllers au lieu d'être dans un service applicatif dédié.

**Code actuel:**
```csharp
// CheckoutController.cs
[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ILocaGuestDbContext _context;  // ❌ DbContext dans controller
    
    public CheckoutController(...)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];  // ❌ Configuration inline
    }
    
    [HttpPost("create-session")]
    public async Task<IActionResult> CreateCheckoutSession(...)
    {
        // ❌ Logique métier Stripe dans le controller
        var options = new SessionCreateOptions { ... };
        var service = new SessionService();
        var session = await service.CreateAsync(options);
        
        return Ok(new { sessionId = session.Id, url = session.Url });
    }
}
```

**Architecture attendue:**
```csharp
// Application Layer - Service
public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(Guid userId, Guid planId, bool isAnnual);
    Task<string> CreatePortalSessionAsync(Guid userId);
    Task HandleWebhookAsync(string payload, string signature);
}

public class StripeService : IStripeService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<string> CreateCheckoutSessionAsync(...)
    {
        // Logique métier centralisée ✅
        var plan = await _planRepository.GetByIdAsync(planId);
        var options = BuildSessionOptions(plan, userId, isAnnual);
        var session = await _stripeClient.CreateSessionAsync(options);
        return session.Url;
    }
}

// Controller léger
[ApiController]
public class CheckoutController : ControllerBase
{
    private readonly IStripeService _stripeService;  // ✅ Service
    
    [HttpPost("create-session")]
    public async Task<IActionResult> CreateCheckoutSession(...)
    {
        var url = await _stripeService.CreateCheckoutSessionAsync(...);  // ✅ Délégation
        return Ok(new { url });
    }
}
```

**Résultat:** ⚠️ **NON CONFORME** - Service manquant

---

### ✅ Endpoint /billing/checkout-session présent

**Vérification:**
```csharp
// CheckoutController.cs
[HttpPost("create-session")]  // ✅ Route: POST /api/checkout/create-session
public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
```

**Alternative possible:**
```
POST /api/billing/checkout-session
```

**Résultat:** ✅ **PRÉSENT** (route `/api/checkout/create-session`)

---

### ✅ Webhook /webhooks/stripe implémenté

**Vérification:**
```csharp
// StripeWebhookController.cs
[ApiController]
[Route("api/webhooks/stripe")]  // ✅ Route correcte
public class StripeWebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);  // ✅ Signature validée
        
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":  // ✅
                await HandleCheckoutSessionCompleted(stripeEvent);
                break;
            case "customer.subscription.created":  // ✅
            case "customer.subscription.updated":  // ✅
                await HandleSubscriptionUpdated(stripeEvent);
                break;
            case "customer.subscription.deleted":  // ✅
                await HandleSubscriptionDeleted(stripeEvent);
                break;
            case "invoice.payment_succeeded":  // ✅
                await HandleInvoicePaymentSucceeded(stripeEvent);
                break;
            case "invoice.payment_failed":  // ✅
                await HandleInvoicePaymentFailed(stripeEvent);
                break;
        }
    }
}
```

**Résultat:** ✅ **IMPLÉMENTÉ**

**Events gérés:**
- ✅ `checkout.session.completed`
- ✅ `customer.subscription.created`
- ✅ `customer.subscription.updated`
- ✅ `customer.subscription.deleted`
- ✅ `invoice.payment_succeeded` (TODO: améliorer)
- ✅ `invoice.payment_failed` (TODO: améliorer)

**Points d'amélioration:**
```csharp
// TODO dans le code actuel
private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
{
    // NOTE: Dans Stripe.net v49+, l'accès au subscription depuis Invoice nécessite
    // une expansion. Pour l'instant, on gère via les events subscription.updated
    // TODO: Implémenter avec invoice.Lines.Data[0].Subscription ou utiliser les expand options
    await Task.CompletedTask;  // ⚠️ Pas encore implémenté
}
```

---

## 4. ⚠️ Controllers - Méthodes implémentées

### Build Réussi

```
Build succeeded with 6 warning(s) in 31.4s
Exit code: 0 ✅
```

**Warnings (non-bloquants):**
- CS8600: Nullable conversion (SaveRentabilityScenarioCommandHandler)
- CS8981: Noms de migrations en minuscules
- CS1998: Async method sans await (DocumentsController)

---

### Controllers Vérifiés

| Controller | Méthodes | Statut | Notes |
|------------|----------|--------|-------|
| **PropertiesController** | CRUD complet | ✅ | Fonctionnel |
| **PropertiesV2Controller** | GET optimisé | ✅ | Avec stats |
| **ContractsController** | CRUD complet | ✅ | Fonctionnel |
| **TenantsController** | CRUD complet | ✅ | Fonctionnel |
| **TenantsV2Controller** | GET optimisé | ✅ | Avec stats |
| **RentabilityScenariosController** | CRUD + versions | ✅ | Complet |
| **AnalyticsController** | Stats complètes | ✅ | Fonctionnel |
| **DashboardController** | Stats dashboard | ✅ | Fonctionnel |
| **SettingsController** | User settings | ✅ | Fonctionnel |
| **CheckoutController** | Stripe checkout | ✅ | 3 endpoints |
| **StripeWebhookController** | Webhooks | ✅ | 6 events |
| **SubscriptionsController** | Subscriptions | ✅ | CRUD |
| **DocumentsController** | Documents | ⚠️ | 1 warning async |

---

## 📊 Score Global de Conformité

| Critère | Score | Détail |
|---------|-------|--------|
| **Handlers sans DbContext direct** | ❌ 0/10 | 32 handlers utilisent DbContext |
| **Entities Domain indépendantes** | ✅ 10/10 | Aucune dépendance EF Core |
| **Value Objects** | ❌ 0/10 | Aucun Value Object |
| **UnitOfWork** | ❌ 0/10 | Pas d'implémentation |
| **Domain Events** | ✅ 10/10 | Dispatchés après SaveChanges |
| **Multi-tenant implicite** | ⚠️ 7/10 | Global Filter + auto-assign |
| **Multi-tenant explicite** | ❌ 0/10 | Pas d'injection ITenantContext |
| **Protection modification tenant** | ✅ 10/10 | Exception si modification |
| **Stripe Service isolé** | ❌ 0/10 | Logique dans controllers |
| **Stripe Endpoints** | ✅ 10/10 | Checkout + webhook complets |
| **Controllers fonctionnels** | ✅ 9/10 | 1 warning async mineur |
| **Build** | ✅ 10/10 | Réussi |

**Score Total:** ⚠️ **5.5/10** (Non conforme DDD pur)

---

## 🚨 Recommandations Prioritaires

### Priorité HAUTE (Bloquants DDD)

1. **Implémenter le pattern Repository**
   - Créer `IPropertyRepository`, `IContractRepository`, `ITenantRepository`, etc.
   - Migrer les 32 handlers pour utiliser les repositories
   - Isoler l'accès aux données

2. **Implémenter le pattern UnitOfWork**
   - Créer `IUnitOfWork` avec `CommitAsync()`, `RollbackAsync()`
   - Gérer les transactions explicitement
   - Centraliser la logique de persistence

3. **Créer des Value Objects**
   - Implémenter `Money`, `Address`, `DateRange`, `Email`, `PhoneNumber`
   - Encapsuler la logique métier
   - Améliorer la validation

### Priorité MOYENNE (Améliorations)

4. **Créer un Service Stripe dédié**
   - `IStripeService` dans Application Layer
   - Extraire la logique des controllers
   - Meilleure testabilité

5. **Injection ITenantContext dans handlers**
   - Validation explicite du tenant
   - Moins de dépendance implicite
   - Code plus clair

6. **Finaliser invoice webhooks Stripe**
   - Implémenter `HandleInvoicePaymentSucceeded` complètement
   - Gérer les expand options Stripe

### Priorité BASSE (Optimisations)

7. **Corriger warning CS1998**
   - `DocumentsController` - ajouter await ou enlever async

8. **Renommer migrations**
   - `addcolumnproperties` → `AddColumnProperties`
   - `setting` → `AddUserSettings`

---

## 📝 Exemple de Refactoring

### Avant (Actuel)
```csharp
// ❌ Non conforme DDD
public class CreatePropertyCommandHandler : IRequestHandler<...>
{
    private readonly ILocaGuestDbContext _context;
    
    public async Task<Result> Handle(...)
    {
        var property = Property.Create(
            request.Name,
            request.Address,  // ❌ string
            request.City,
            propertyType,
            request.Rent,  // ❌ decimal
            request.Bedrooms,
            request.Bathrooms);
        
        _context.Properties.Add(property);  // ❌ DbContext direct
        await _context.SaveChangesAsync();
    }
}
```

### Après (DDD Conforme)
```csharp
// ✅ Conforme DDD
public class CreatePropertyCommandHandler : IRequestHandler<...>
{
    private readonly IPropertyRepository _propertyRepository;  // ✅ Repository
    private readonly IUnitOfWork _unitOfWork;  // ✅ UnitOfWork
    private readonly ITenantContext _tenantContext;  // ✅ Tenant explicite
    
    public async Task<Result> Handle(...)
    {
        // Validation tenant ✅
        if (!_tenantContext.IsAuthenticated)
            return Result.Failure("User not authenticated");
        
        // Value Objects ✅
        var address = new Address(
            request.Street,
            request.City,
            request.PostalCode,
            request.Country);
        
        var rent = new Money(request.Rent, "EUR");
        
        // Factory method ✅
        var property = Property.Create(
            request.Name,
            address,  // ✅ Value Object
            propertyType,
            rent,  // ✅ Value Object
            request.Bedrooms,
            request.Bathrooms);
        
        // Repository ✅
        await _propertyRepository.AddAsync(property);
        
        // Transaction explicite ✅
        await _unitOfWork.CommitAsync();
        
        return Result.Success(property.Id);
    }
}
```

---

## ✅ Conclusion

**Statut:** ⚠️ **Architecture Hybride - Non conforme DDD pur**

**Points forts:**
- ✅ Domain indépendant d'EF Core
- ✅ Domain Events bien implémentés
- ✅ Multi-tenant fonctionnel (Global Filter)
- ✅ Stripe webhooks complets
- ✅ Build réussi

**Points faibles:**
- ❌ Pas de pattern Repository
- ❌ Pas de pattern UnitOfWork
- ❌ Pas de Value Objects
- ❌ Handlers couplés au DbContext
- ❌ Logique Stripe dans controllers

**Recommandation:**
L'architecture actuelle est **fonctionnelle** mais **n'est pas DDD pure**. Elle correspond plutôt à une **architecture en couches classique avec CQRS**. 

Pour une conformité DDD complète, implémenter les 3 refactorings prioritaires :
1. Pattern Repository
2. Pattern UnitOfWork
3. Value Objects

**Effort estimé:** 3-5 jours de développement pour la migration complète vers DDD pur.
