# 📦 Système d'Abonnement LocaGuest - Documentation Complète

## 📑 Table des Matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture](#architecture)
3. [Modèle de Données](#modèle-de-données)
4. [Backend](#backend)
5. [Frontend](#frontend)
6. [Flux de Données](#flux-de-données)
7. [Utilisation](#utilisation)
8. [Configuration Stripe](#configuration-stripe)

---

## 🎯 Vue d'ensemble

Le système d'abonnement de LocaGuest permet de monétiser l'application en proposant 4 plans tarifaires avec des fonctionnalités et quotas différenciés.

### **Plans Disponibles**

| Plan | Prix | Scénarios | Exports | IA | Partages |
|------|------|-----------|---------|-----|----------|
| **Free** | 0€ | 3 | 5/mois | 2/mois | 1 view |
| **Pro** | 19€/mois | 50 | ∞ | ∞ | 5 edit |
| **Business** | 49€/mois | 200 | ∞ | ∞ | 10 edit |
| **Enterprise** | Custom | ∞ | ∞ | ∞ | ∞ |

---

## 🏗️ Architecture

```
Frontend (Angular 18)
  ├─ SubscriptionService (Signals)
  ├─ Guards (featureGuard)
  ├─ Directives (*ifFeature)
  ├─ Pages (Pricing, Upgrade Modal)
  └─ HTTP Client → API

Backend (.NET 9)
  ├─ API Controllers
  │   ├─ SubscriptionsController
  │   ├─ CheckoutController
  │   └─ StripeWebhookController
  ├─ Application Layer
  │   ├─ ISubscriptionService
  │   └─ Attributes ([RequiresFeature], [RequiresQuota])
  ├─ Domain Layer
  │   ├─ Plan (Aggregate)
  │   ├─ Subscription (Aggregate)
  │   ├─ UsageEvent
  │   └─ UsageAggregate
  └─ Infrastructure
      ├─ DbContext (PostgreSQL)
      └─ PlanSeeder

Stripe
  ├─ Products & Prices
  ├─ Checkout Sessions
  └─ Webhooks
```

---

## 🗄️ Modèle de Données

### **Plan**
```csharp
- Name: string                    // "Pro", "Business"
- Code: string                    // "pro", "business"
- MonthlyPrice: decimal           // 19.00
- AnnualPrice: decimal            // 182.40 (-20%)
- Limits: Dictionary<string, int> // { "scenarios": 50 }
- Features: List<string>          // ["api_access", "private_templates"]
- StripeMonthlyPriceId: string?
- StripeAnnualPriceId: string?
```

### **Subscription**
```csharp
- UserId: Guid
- PlanId: Guid
- Status: string                  // "trialing", "active", "past_due", "canceled"
- CurrentPeriodStart: DateTime
- CurrentPeriodEnd: DateTime
- TrialEndsAt: DateTime?
- StripeCustomerId: string?
- StripeSubscriptionId: string?
```

### **UsageEvent**
```csharp
- SubscriptionId: Guid
- UserId: Guid
- Dimension: string               // "scenarios", "exports", "ai_suggestions"
- Value: int                      // 1
- Timestamp: DateTime
```

### **UsageAggregate**
```csharp
- Dimension: string
- Period: string                  // "2025-11"
- TotalValue: int                 // Somme des events
```

---

## ⚙️ Backend

### **1. Feature Gating**

Protéger un endpoint avec `[RequiresFeature]`:

```csharp
[HttpGet("api-docs")]
[RequiresFeature("api_access")]
public async Task<IActionResult> GetApiDocs()
{
    return Ok(apiDocs);
}
```

**Comment ça marche**:
1. L'attribut intercepte la requête
2. Récupère l'userId depuis JWT claims
3. Appelle `ISubscriptionService.HasFeatureAsync(userId, "api_access")`
4. Si false → 403 Forbidden
5. Si true → continue l'exécution

### **2. Quota Checking**

Protéger un endpoint avec `[RequiresQuota]`:

```csharp
[HttpPost("scenarios")]
[RequiresQuota("scenarios")]
public async Task<IActionResult> CreateScenario(CreateScenarioCommand cmd)
{
    var result = await _mediator.Send(cmd);
    return Ok(result);
}
```

**Comment ça marche**:
1. Vérifie quota AVANT l'exécution
2. Si quota dépassé → 429 Too Many Requests
3. Si OK → exécute l'action
4. Enregistre l'usage APRÈS le succès

### **3. SubscriptionService**

```csharp
public class SubscriptionService : ISubscriptionService
{
    // Récupère l'abonnement actif d'un utilisateur
    Task<Subscription?> GetUserSubscriptionAsync(Guid userId);
    
    // Vérifie si l'utilisateur a accès à une feature
    Task<bool> HasFeatureAsync(Guid userId, string feature);
    
    // Vérifie si l'utilisateur a du quota restant
    Task<bool> CheckQuotaAsync(Guid userId, string dimension);
    
    // Enregistre un événement d'usage
    Task RecordUsageAsync(Guid userId, string dimension, int value = 1);
    
    // Récupère l'usage détaillé
    Task<Dictionary<string, UsageDto>> GetUsageAsync(Guid userId);
}
```

**Exemple CheckQuota**:
```csharp
public async Task<bool> CheckQuotaAsync(Guid userId, string dimension)
{
    // 1. Récupérer subscription avec plan
    var subscription = await _context.Subscriptions
        .Include(s => s.Plan)
        .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive());
    
    if (subscription == null) return false;
    
    // 2. Récupérer la limite
    var limit = subscription.Plan.GetLimit(dimension);
    if (subscription.Plan.IsUnlimited(dimension)) return true;
    
    // 3. Calculer l'usage actuel (mois en cours)
    var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
    var usage = await _context.UsageAggregates
        .Where(u => u.SubscriptionId == subscription.Id 
                 && u.Dimension == dimension 
                 && u.Period == currentPeriod)
        .SumAsync(u => u.TotalValue);
    
    // 4. Comparer
    return usage < limit;
}
```

### **4. Stripe Integration**

#### **CheckoutController**

Créer une session Stripe:

```csharp
[HttpPost("create-session")]
public async Task<IActionResult> CreateCheckoutSession(CreateCheckoutRequest request)
{
    var plan = await _context.Plans.FindAsync(request.PlanId);
    var priceId = request.IsAnnual ? plan.StripeAnnualPriceId : plan.StripeMonthlyPriceId;
    
    var options = new SessionCreateOptions
    {
        LineItems = new List<SessionLineItemOptions>
        {
            new() { Price = priceId, Quantity = 1 }
        },
        Mode = "subscription",
        SuccessUrl = "http://localhost:4201/subscription/success?session_id={CHECKOUT_SESSION_ID}",
        CancelUrl = "http://localhost:4201/pricing",
        SubscriptionData = new SessionSubscriptionDataOptions
        {
            TrialPeriodDays = 14,
            Metadata = new Dictionary<string, string>
            {
                { "user_id", userId.ToString() },
                { "plan_id", plan.Id.ToString() }
            }
        }
    };
    
    var service = new SessionService();
    var session = await service.CreateAsync(options);
    
    return Ok(new { sessionId = session.Id, url = session.Url });
}
```

#### **StripeWebhookController**

Gérer les événements Stripe:

```csharp
[HttpPost]
public async Task<IActionResult> HandleWebhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var stripeSignature = Request.Headers["Stripe-Signature"];
    var webhookSecret = _configuration["Stripe:WebhookSecret"];
    
    var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
    
    switch (stripeEvent.Type)
    {
        case "checkout.session.completed":
            await HandleCheckoutCompleted(stripeEvent);
            break;
        
        case "customer.subscription.updated":
            await HandleSubscriptionUpdated(stripeEvent);
            break;
        
        case "customer.subscription.deleted":
            await HandleSubscriptionDeleted(stripeEvent);
            break;
    }
    
    return Ok();
}

private async Task HandleCheckoutCompleted(Event stripeEvent)
{
    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
    var userId = Guid.Parse(session.Metadata["user_id"]);
    var planId = Guid.Parse(session.Metadata["plan_id"]);
    
    var subscription = Subscription.Create(userId, planId, isAnnual: false, trialDays: 14);
    subscription.SetStripeInfo(session.CustomerId, session.SubscriptionId);
    
    _context.Subscriptions.Add(subscription);
    await _context.SaveChangesAsync();
}
```

---

## 🎨 Frontend

### **1. SubscriptionService (Angular)**

```typescript
@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  // Signals
  private currentPlanSignal = signal<Plan | null>(null);
  private usageDataSignal = signal<Record<string, UsageDto>>({});
  
  // Computed
  currentPlan = computed(() => this.currentPlanSignal());
  usageData = computed(() => this.usageDataSignal());
  isFreePlan = computed(() => this.currentPlan()?.code === 'free');
  
  // Initialisation
  async initialize() {
    await this.loadCurrentSubscription();
    this.loadPlans();
    this.loadUsage();
  }
  
  // Vérifier feature
  canAccessFeature(featureName: string): Observable<boolean> {
    const plan = this.currentPlan();
    if (!plan) return of(false);
    
    if (plan.features.includes(featureName)) return of(true);
    
    return this.http.get<{ hasAccess: boolean }>(
      `${this.baseUrl}/features/${featureName}`
    ).pipe(map(r => r.hasAccess));
  }
  
  // Vérifier quota
  checkQuota(dimension: string): Observable<boolean> {
    const usage = this.usageData()[dimension];
    if (usage?.unlimited) return of(true);
    if (usage && usage.current < usage.limit) return of(true);
    
    return this.http.get<{ hasQuota: boolean }>(
      `${this.baseUrl}/quota/${dimension}`
    ).pipe(map(r => r.hasQuota));
  }
}
```

### **2. Feature Guard**

```typescript
export const featureGuard = (feature: string): CanActivateFn => {
  return () => {
    const service = inject(SubscriptionService);
    const router = inject(Router);
    
    return service.canAccessFeature(feature).pipe(
      map(hasAccess => {
        if (hasAccess) return true;
        router.navigate(['/pricing'], { queryParams: { feature } });
        return false;
      })
    );
  };
};

// Utilisation dans routes
{
  path: 'api-docs',
  canActivate: [featureGuard('api_access')]
}
```

### **3. Directive *ifFeature**

```typescript
@Directive({ selector: '[ifFeature]' })
export class IfFeatureDirective {
  @Input() set ifFeature(feature: string) {
    const plan = this.subscriptionService.currentPlan();
    effect(() => {
      if (plan?.features.includes(feature)) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      } else {
        this.viewContainer.clear();
      }
    });
  }
}

// Utilisation
<button *ifFeature="'api_access'">API Docs</button>
```

---

## 🔄 Flux de Données

### **1. Création d'un Scénario (avec quota)**

```
User clicks "Create Scenario"
  ↓
Frontend: checkQuota('scenarios')
  ↓
API: GET /api/subscriptions/quota/scenarios
  ↓
Backend: SubscriptionService.CheckQuotaAsync()
  - Récupère subscription + plan
  - Calcule usage actuel
  - Compare avec limite
  ↓
Response: { hasQuota: true/false }
  ↓
if true:
  POST /api/scenarios (with [RequiresQuota("scenarios")])
    ↓
  Create scenario
    ↓
  Record usage event
    ↓
  Update aggregate
    ↓
  Response: 201 Created
else:
  Show UpgradeModal
```

### **2. Accès à une Feature Premium**

```
User navigates to /api-docs
  ↓
Router: featureGuard('api_access')
  ↓
Frontend: canAccessFeature('api_access')
  ↓
Check local: plan.features.includes('api_access')
  ↓
if true: Allow navigation
if false: Redirect to /pricing?feature=api_access
```

### **3. Souscription à un Plan**

```
User clicks "Choose Pro Plan"
  ↓
POST /api/checkout/create-session
  { planId: "...", isAnnual: false }
  ↓
Backend creates Stripe Checkout Session
  - Trial: 14 days
  - Metadata: user_id, plan_id
  ↓
Response: { url: "https://checkout.stripe.com/..." }
  ↓
Redirect to Stripe
  ↓
User completes payment
  ↓
Stripe sends webhook: checkout.session.completed
  ↓
Backend: StripeWebhookController.HandleCheckoutCompleted()
  - Create Subscription in DB
  - Status: "trialing"
  - TrialEndsAt: +14 days
  ↓
After 14 days:
  Stripe sends: invoice.payment_succeeded
  ↓
  Backend: Update subscription.Status = "active"
```

---

## 📝 Utilisation Pratique

### **Backend: Protéger un endpoint**

```csharp
// Feature gating
[HttpGet("templates/private")]
[RequiresFeature("private_templates")]
public IActionResult GetPrivateTemplates()
{
    return Ok(_templates.Where(t => t.IsPrivate));
}

// Quota gating
[HttpPost("scenarios")]
[RequiresQuota("scenarios")]
public async Task<IActionResult> CreateScenario(CreateScenarioCommand cmd)
{
    var result = await _mediator.Send(cmd);
    return Ok(result);
}
```

### **Frontend: Protéger une route**

```typescript
export const routes: Routes = [
  {
    path: 'api-docs',
    canActivate: [featureGuard('api_access')],
    loadComponent: () => import('./pages/api-docs')
  }
];
```

### **Frontend: Affichage conditionnel**

```html
<!-- Avec directive -->
<button *ifFeature="'unlimited_exports'">
  Export Illimité
</button>

<!-- Avec computed -->
<div *ngIf="subscriptionService.isProPlan()">
  Pro Features
</div>

<!-- Vérifier quota avant action -->
<button (click)="onCreateScenario()">
  Créer un scénario
</button>
```

```typescript
async onCreateScenario() {
  const hasQuota = await firstValueFrom(
    this.subscriptionService.checkQuota('scenarios')
  );
  
  if (!hasQuota) {
    this.showUpgradeModal = true;
    return;
  }
  
  await this.create();
}
```

---

## ⚙️ Configuration Stripe

### **1. Créer les Products**

Via Dashboard: https://dashboard.stripe.com/test/products

```
Product 1: LocaGuest Pro
  Price: 19 EUR/month → price_xxx
  Price: 182.40 EUR/year → price_yyy

Product 2: LocaGuest Business
  Price: 49 EUR/month → price_zzz
  Price: 470.40 EUR/year → price_aaa
```

### **2. Configurer appsettings.json**

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "SuccessUrl": "http://localhost:4201/subscription/success",
    "CancelUrl": "http://localhost:4201/pricing"
  }
}
```

### **3. Tester avec Stripe CLI**

```bash
# Installer
stripe login

# Forward webhooks
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe

# Trigger events
stripe trigger checkout.session.completed
stripe trigger customer.subscription.updated
```

---

## 📊 Résumé

**Backend**: 3 controllers, 4 entities, 2 attributes, 1 service  
**Frontend**: 1 service, 1 guard, 1 directive, 2 pages  
**Database**: 4 tables (plans, subscriptions, usage_events, usage_aggregates)  
**Stripe**: Products, Prices, Checkout, Webhooks  

**Total**: ~3000 lignes de code production-ready 🚀
