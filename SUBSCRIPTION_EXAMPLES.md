# 💡 Exemples d'Utilisation - Système d'Abonnement

## 📚 Table des Matières

1. [Cas d'Usage Backend](#cas-dusage-backend)
2. [Cas d'Usage Frontend](#cas-dusage-frontend)
3. [Scénarios Complets](#scénarios-complets)
4. [Tests](#tests)
5. [Troubleshooting](#troubleshooting)

---

## 🔧 Cas d'Usage Backend

### **1. Créer un nouvel endpoint protégé par feature**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    // Accessible uniquement aux plans Business et Enterprise
    [HttpGet("advanced")]
    [RequiresFeature("advanced_analytics")]
    public async Task<IActionResult> GetAdvancedAnalytics()
    {
        var analytics = await _analyticsService.GetAdvancedMetrics();
        return Ok(analytics);
    }
    
    // Accessible à tous mais avec feature optionnelle
    [HttpGet("basic")]
    public async Task<IActionResult> GetBasicAnalytics()
    {
        var hasAdvanced = await _subscriptionService.HasFeatureAsync(GetUserId(), "advanced_analytics");
        
        var analytics = hasAdvanced 
            ? await _analyticsService.GetAdvancedMetrics()
            : await _analyticsService.GetBasicMetrics();
            
        return Ok(analytics);
    }
}
```

### **2. Créer un endpoint avec quota**

```csharp
[HttpPost("export/pdf")]
[RequiresQuota("exports")]
public async Task<IActionResult> ExportToPdf([FromBody] ExportRequest request)
{
    var pdf = await _exportService.GeneratePdf(request);
    
    // Le quota est automatiquement enregistré après succès
    return File(pdf, "application/pdf", "export.pdf");
}
```

### **3. Vérifier manuellement un quota avant traitement lourd**

```csharp
[HttpPost("reports/generate")]
public async Task<IActionResult> GenerateReport([FromBody] ReportRequest request)
{
    var userId = GetUserId();
    
    // Vérifier le quota avant de démarrer le traitement
    var hasQuota = await _subscriptionService.CheckQuotaAsync(userId, "reports");
    if (!hasQuota)
    {
        return StatusCode(429, new 
        { 
            error = "Quota exceeded",
            message = "Vous avez atteint la limite de rapports pour ce mois",
            upgradeUrl = "/pricing"
        });
    }
    
    // Traitement long
    var report = await _reportService.GenerateAsync(request);
    
    // Enregistrer l'usage après succès
    await _subscriptionService.RecordUsageAsync(userId, "reports", 1);
    
    return Ok(report);
}
```

### **4. Créer un plan custom pour Enterprise**

```csharp
public class CustomPlanService
{
    public async Task<Plan> CreateEnterprisePlanAsync(EnterpriseCustomization customization)
    {
        var plan = Plan.Create(
            name: $"Enterprise - {customization.CompanyName}",
            code: $"enterprise_{customization.CompanyName.ToLower()}",
            description: "Plan Enterprise personnalisé",
            monthlyPrice: customization.NegotiatedPrice,
            annualPrice: customization.NegotiatedPrice * 10 // -17%
        );
        
        // Limites personnalisées
        plan.SetLimit("scenarios", customization.MaxScenarios ?? 999999);
        plan.SetLimit("workspaces", customization.MaxWorkspaces ?? 999999);
        plan.SetLimit("team_members", customization.MaxTeamMembers ?? 999999);
        
        // Features personnalisées
        plan.AddFeatures(new[]
        {
            "api_read_write",
            "sla_99_9",
            "dedicated_support",
            "custom_integrations",
            "white_label"
        });
        
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();
        
        return plan;
    }
}
```

### **5. Gérer les migrations de plan**

```csharp
[HttpPost("subscriptions/upgrade")]
public async Task<IActionResult> UpgradePlan([FromBody] UpgradeRequest request)
{
    var userId = GetUserId();
    var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
    
    if (subscription == null)
        return NotFound("No active subscription");
    
    var newPlan = await _context.Plans.FindAsync(request.NewPlanId);
    if (newPlan == null)
        return NotFound("Plan not found");
    
    // Vérifier que c'est bien un upgrade
    if (newPlan.MonthlyPrice <= subscription.Plan.MonthlyPrice)
        return BadRequest("Cannot downgrade via this endpoint");
    
    // Créer une session Stripe pour le changement
    var options = new SessionCreateOptions
    {
        Mode = "subscription",
        LineItems = new List<SessionLineItemOptions>
        {
            new() 
            { 
                Price = request.IsAnnual ? newPlan.StripeAnnualPriceId : newPlan.StripeMonthlyPriceId,
                Quantity = 1
            }
        },
        SubscriptionData = new SessionSubscriptionDataOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "upgrade_from", subscription.PlanId.ToString() },
                { "subscription_id", subscription.Id.ToString() }
            }
        },
        SuccessUrl = _configuration["Stripe:SuccessUrl"],
        CancelUrl = _configuration["Stripe:CancelUrl"]
    };
    
    var service = new SessionService();
    var session = await service.CreateAsync(options);
    
    return Ok(new { url = session.Url });
}
```

### **6. Créer des rapports d'usage**

```csharp
[HttpGet("admin/usage-report")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetUsageReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
{
    var report = await _context.UsageAggregates
        .Where(u => u.PeriodStart >= startDate && u.PeriodEnd <= endDate)
        .GroupBy(u => new { u.Dimension, Period = u.Period })
        .Select(g => new
        {
            Dimension = g.Key.Dimension,
            Period = g.Key.Period,
            TotalUsage = g.Sum(u => u.TotalValue),
            UniqueUsers = g.Select(u => u.UserId).Distinct().Count()
        })
        .ToListAsync();
    
    return Ok(report);
}
```

---

## 🎨 Cas d'Usage Frontend

### **1. Vérifier une feature avant d'afficher un bouton**

```typescript
@Component({
  template: `
    <div class="actions">
      @if (canExport()) {
        <button (click)="export()">Exporter</button>
      }
      
      @if (!canExport()) {
        <button (click)="showUpgrade()">
          🔒 Exporter (Pro)
        </button>
      }
    </div>
  `
})
export class MyComponent {
  private subscriptionService = inject(SubscriptionService);
  
  canExport = computed(() => {
    const plan = this.subscriptionService.currentPlan();
    return plan?.features.includes('unlimited_exports') ?? false;
  });
  
  showUpgrade() {
    // Afficher modal upgrade
  }
}
```

### **2. Vérifier un quota avec feedback visuel**

```typescript
@Component({
  template: `
    <div class="quota-indicator">
      <div class="progress-bar">
        <div class="fill" [style.width.%]="usagePercent()"></div>
      </div>
      <span>{{ usage().current }} / {{ usage().limit }}</span>
      
      @if (usage().current >= usage().limit) {
        <button (click)="upgrade()">Upgrade</button>
      }
    </div>
  `
})
export class QuotaIndicatorComponent {
  private subscriptionService = inject(SubscriptionService);
  
  dimension = input.required<string>();
  
  usage = computed(() => {
    return this.subscriptionService.getUsage(this.dimension()) ?? {
      current: 0,
      limit: 0,
      unlimited: false
    };
  });
  
  usagePercent = computed(() => {
    const u = this.usage();
    if (u.unlimited) return 0;
    return Math.min((u.current / u.limit) * 100, 100);
  });
}
```

### **3. Implémenter un paywall conditionnel**

```typescript
@Component({
  template: `
    @if (!isBlocked()) {
      <ng-content></ng-content>
    }
    
    @if (isBlocked()) {
      <div class="paywall">
        <h3>🔒 Fonctionnalité Premium</h3>
        <p>{{ message() }}</p>
        <button (click)="goToPricing()">Upgrade</button>
      </div>
    }
  `
})
export class PaywallComponent {
  private subscriptionService = inject(SubscriptionService);
  private router = inject(Router);
  
  requiredFeature = input<string>();
  message = input<string>('Cette fonctionnalité nécessite un plan supérieur');
  
  isBlocked = computed(() => {
    const feature = this.requiredFeature();
    if (!feature) return false;
    
    const plan = this.subscriptionService.currentPlan();
    return !plan?.features.includes(feature);
  });
  
  goToPricing() {
    this.router.navigate(['/pricing'], {
      queryParams: { feature: this.requiredFeature() }
    });
  }
}

// Utilisation
<app-paywall [requiredFeature]="'api_access'">
  <app-api-documentation />
</app-paywall>
```

### **4. Créer un composant de comparaison de plans**

```typescript
@Component({
  template: `
    <div class="comparison-table">
      <table>
        <thead>
          <tr>
            <th>Feature</th>
            @for (plan of plans(); track plan.id) {
              <th>{{ plan.name }}</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (feature of allFeatures; track feature) {
            <tr>
              <td>{{ feature.label }}</td>
              @for (plan of plans(); track plan.id) {
                <td>
                  @if (plan.features.includes(feature.code)) {
                    ✓
                  } @else {
                    ✗
                  }
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class PlanComparisonComponent {
  private subscriptionService = inject(SubscriptionService);
  
  plans = this.subscriptionService.availablePlans;
  
  allFeatures = [
    { code: 'unlimited_exports', label: 'Exports illimités' },
    { code: 'api_access', label: 'Accès API' },
    { code: 'private_templates', label: 'Templates privés' },
    { code: 'advanced_analytics', label: 'Analytics avancés' },
    { code: 'team_collaboration', label: 'Collaboration équipe' },
    { code: 'sso', label: 'SSO' },
    { code: 'dedicated_support', label: 'Support dédié' }
  ];
}
```

### **5. Implémenter un countdown pour la fin du trial**

```typescript
@Component({
  template: `
    @if (isInTrial() && daysLeft() <= 3) {
      <div class="trial-banner">
        ⚠️ Votre essai se termine dans {{ daysLeft() }} jour(s)
        <button (click)="subscribe()">S'abonner maintenant</button>
      </div>
    }
  `
})
export class TrialBannerComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);
  
  subscription = this.subscriptionService.currentSubscription;
  
  isInTrial = computed(() => {
    const sub = this.subscription();
    return sub?.status === 'trialing';
  });
  
  daysLeft = computed(() => {
    const sub = this.subscription();
    if (!sub?.trialEndsAt) return 0;
    
    const now = new Date();
    const trialEnd = new Date(sub.trialEndsAt);
    const diff = trialEnd.getTime() - now.getTime();
    
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  });
  
  subscribe() {
    this.router.navigate(['/pricing']);
  }
}
```

---

## 🎬 Scénarios Complets

### **Scénario 1: Utilisateur Free veut créer son 4ème scénario**

```
1. User clique "Créer un scénario"
   
2. Frontend appelle checkQuota('scenarios')
   const hasQuota = await this.subscriptionService.checkQuota('scenarios').toPromise();
   
3. Backend vérifie:
   - Plan: Free (limite: 3)
   - Usage actuel: 3
   - Résultat: 3 >= 3 → false
   
4. Frontend reçoit false
   
5. Afficher UpgradeModal
   <app-upgrade-modal
     [title]="'Limite atteinte'"
     [message]="'Passez à Pro pour créer jusqu\'à 50 scénarios'"
     [feature]="'scenarios'"
   />
   
6. User clique "Voir les plans"
   
7. Redirection vers /pricing?feature=scenarios
   
8. User sélectionne Pro (19€/mois)
   
9. Création session Stripe → Paiement
   
10. Webhook checkout.session.completed
   
11. Backend crée Subscription (status: trialing, 14 jours)
   
12. User peut maintenant créer jusqu'à 50 scénarios
```

### **Scénario 2: Utilisateur Pro veut accéder à l'API**

```
1. User navigue vers /api-docs
   
2. Router vérifie avec featureGuard('api_access')
   
3. Frontend vérifie le plan actuel:
   - Plan: Pro
   - Features: ['private_templates', 'comparison', 'unlimited_exports']
   - api_access: NON présent
   
4. Redirection vers /pricing?feature=api_access
   
5. Message: "L'accès API est disponible à partir du plan Business"
   
6. User upgrade vers Business
   
7. Après paiement, accès à /api-docs débloqué
```

### **Scénario 3: Migration automatique après fin de trial**

```
Jour 1: User s'inscrit, checkout avec plan Pro
  → Subscription créé (status: trialing, trial: 14 jours)
  → Accès complet aux features Pro

Jour 14: Fin du trial
  → Stripe facture automatiquement
  → Webhook: invoice.payment_succeeded
  → Backend: subscription.Status = "active"

Si paiement échoue:
  → Webhook: invoice.payment_failed
  → Backend: subscription.Status = "past_due"
  → Frontend affiche banner: "Problème de paiement"
```

---

## 🧪 Tests

### **Test Backend: Vérification de quota**

```csharp
[Fact]
public async Task CheckQuota_WhenUnderLimit_ReturnsTrue()
{
    // Arrange
    var userId = Guid.NewGuid();
    var plan = Plan.Create("Pro", "pro", "Pro plan", 19, 182.40);
    plan.SetLimit("scenarios", 50);
    
    var subscription = Subscription.Create(userId, plan.Id);
    
    _context.Plans.Add(plan);
    _context.Subscriptions.Add(subscription);
    await _context.SaveChangesAsync();
    
    // Simuler 10 usages
    for (int i = 0; i < 10; i++)
    {
        await _service.RecordUsageAsync(userId, "scenarios", 1);
    }
    
    // Act
    var hasQuota = await _service.CheckQuotaAsync(userId, "scenarios");
    
    // Assert
    Assert.True(hasQuota); // 10 < 50
}

[Fact]
public async Task CheckQuota_WhenOverLimit_ReturnsFalse()
{
    // ... setup avec limite 3 et 3 usages
    var hasQuota = await _service.CheckQuotaAsync(userId, "scenarios");
    Assert.False(hasQuota); // 3 >= 3
}
```

### **Test Frontend: Feature Guard**

```typescript
describe('FeatureGuard', () => {
  it('should allow access when user has feature', (done) => {
    const subscriptionService = jasmine.createSpyObj('SubscriptionService', ['canAccessFeature']);
    subscriptionService.canAccessFeature.and.returnValue(of(true));
    
    TestBed.configureTestingModule({
      providers: [
        { provide: SubscriptionService, useValue: subscriptionService }
      ]
    });
    
    const guard = featureGuard('api_access');
    const result = guard({} as any, {} as any);
    
    (result as Observable<boolean>).subscribe(allowed => {
      expect(allowed).toBe(true);
      done();
    });
  });
  
  it('should redirect when user does not have feature', (done) => {
    const subscriptionService = jasmine.createSpyObj('SubscriptionService', ['canAccessFeature']);
    subscriptionService.canAccessFeature.and.returnValue(of(false));
    
    const router = jasmine.createSpyObj('Router', ['navigate']);
    
    TestBed.configureTestingModule({
      providers: [
        { provide: SubscriptionService, useValue: subscriptionService },
        { provide: Router, useValue: router }
      ]
    });
    
    const guard = featureGuard('api_access');
    const result = guard({} as any, {} as any);
    
    (result as Observable<boolean>).subscribe(allowed => {
      expect(allowed).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/pricing'], {
        queryParams: { feature: 'api_access' }
      });
      done();
    });
  });
});
```

---

## 🔧 Troubleshooting

### **Problème: Feature check retourne toujours false**

**Diagnostic**:
```csharp
// Ajouter des logs
_logger.LogInformation("Checking feature {Feature} for user {UserId}", feature, userId);
_logger.LogInformation("User plan: {PlanCode}, Features: {Features}", 
    subscription.Plan.Code, 
    string.Join(", ", subscription.Plan.Features));
```

**Solution**: Vérifier que la feature est bien dans `plan.Features` dans la base de données.

### **Problème: Quota toujours dépassé même avec usage faible**

**Diagnostic**:
```sql
-- Vérifier les agrégats
SELECT dimension, period, total_value, period_start, period_end
FROM usage_aggregates
WHERE user_id = 'xxx'
AND dimension = 'scenarios'
ORDER BY period_start DESC;

-- Vérifier le plan
SELECT limits
FROM plans
WHERE id = (SELECT plan_id FROM subscriptions WHERE user_id = 'xxx');
```

**Solution**: Reset des agrégats du mois si besoin:
```csharp
await _context.UsageAggregates
    .Where(u => u.UserId == userId && u.Period == DateTime.UtcNow.ToString("yyyy-MM"))
    .ExecuteDeleteAsync();
```

### **Problème: Webhook Stripe non reçu**

**Diagnostic**:
```bash
# Vérifier que le forwarding fonctionne
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe

# Tester manuellement
stripe trigger checkout.session.completed
```

**Solution**: 
1. Vérifier webhook secret dans appsettings.json
2. Vérifier que l'API est accessible
3. Check les logs Stripe Dashboard

### **Problème: Signal ne se met pas à jour**

**Diagnostic**:
```typescript
effect(() => {
  console.log('Current plan changed:', this.subscriptionService.currentPlan());
});
```

**Solution**: 
- Vérifier que `loadCurrentSubscription()` est bien appelé
- Forcer le refresh: `await this.subscriptionService.refresh()`

---

## 📚 Ressources

- [Stripe API Docs](https://stripe.com/docs/api)
- [Angular Signals Guide](https://angular.dev/guide/signals)
- [EF Core Best Practices](https://learn.microsoft.com/en-us/ef/core/)
- [Feature Flags Pattern](https://martinfowler.com/articles/feature-toggles.html)

---

**Total: 20+ exemples pratiques pour tous les cas d'usage** 🎯
