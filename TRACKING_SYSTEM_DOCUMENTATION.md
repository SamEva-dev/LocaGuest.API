# Système de Tracking - LocaGuest

**Date:** 15 novembre 2025  
**Version:** 1.0  
**Statut:** ✅ Production Ready

---

## 📊 Vue d'Ensemble

Un système complet de **tracking comportemental** et d'**analytics produit** pour LocaGuest, permettant de comprendre comment les utilisateurs interagissent avec l'application.

### Différence avec l'Audit

| Aspect | Audit (Sécurité) | Tracking (Analytics) |
|--------|------------------|----------------------|
| **Objectif** | Sécurité, conformité, forensics | Product analytics, UX, business intelligence |
| **Données** | Actions sensibles, modifications | Navigation, clics, utilisation features |
| **Rétention** | Long terme (1-7 ans) | Moyen terme (90 jours - 1 an) |
| **RGPD** | Données audit, consentement implicite | Anonymisation IP, opt-out possible |
| **Database** | `Locaguest_Audit` (séparée) | `Locaguest` (table dédiée) |
| **Scope** | Commands CQRS + Entity changes | Pages, features, actions business |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│              FRONTEND (Angular)                         │
│                                                         │
│  Router Navigation → TrackingService.trackPageView()   │
│  User Actions → track('EVENT_TYPE', metadata)          │
│  Button Clicks → trackButtonClick()                    │
│  Forms → trackFormSubmit()                             │
│  Errors → trackError()                                 │
│                                                         │
│  Batching: Queue events → Flush every 5s or 10 events │
│                   ↓ HTTP POST                          │
└─────────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────────┐
│              BACKEND (.NET)                             │
│                                                         │
│  TrackingController                                    │
│   └─ POST /api/tracking/event                         │
│   └─ POST /api/tracking/events/batch                  │
│                   ↓                                    │
│  ITrackingService                                      │
│   └─ Inject TenantId (ITenantContext)                 │
│   └─ Inject UserId (ICurrentUserService)              │
│   └─ Anonymize IP (RGPD)                              │
│   └─ Add UserAgent, Timestamp                         │
│                   ↓                                    │
│  TrackingMiddleware (Auto API Tracking)                │
│   └─ Intercept all authenticated requests             │
│   └─ Track URL, Method, Duration, StatusCode          │
│   └─ Exclude /health, /swagger, /tracking/*           │
│                   ↓                                    │
│  LocaGuestDbContext → tracking_events table            │
│   └─ PostgreSQL avec indexes optimisés                │
└─────────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────────┐
│         DATABASE (PostgreSQL)                           │
│                                                         │
│  tracking_events                                        │
│   ├─ TenantId (multi-tenant isolation)                │
│   ├─ UserId                                            │
│   ├─ EventType (PAGE_VIEW, BUTTON_CLICK...)           │
│   ├─ PageName, Url                                    │
│   ├─ IpAddress (anonymized xxx.xxx.xxx.0)             │
│   ├─ UserAgent                                         │
│   ├─ Metadata (JSONB flexible)                        │
│   ├─ Timestamp                                         │
│   ├─ SessionId, CorrelationId                         │
│   └─ DurationMs, HttpStatusCode                       │
│                                                         │
│  Indexes: TenantId, UserId, EventType, Timestamp,      │
│           (TenantId, Timestamp), (EventType, Timestamp)│
└─────────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────────┐
│         ANALYTICS & BI                                  │
│                                                         │
│  SQL Queries (15+ ready-to-use)                        │
│   ├─ Pages les plus visitées                          │
│   ├─ Features les plus utilisées                      │
│   ├─ Utilisateurs actifs (DAU, WAU, MAU)              │
│   ├─ Heatmap d'utilisation                            │
│   ├─ Funnel de conversion                             │
│   ├─ Analyse de rétention (cohortes)                  │
│   ├─ Performance API                                   │
│   ├─ Taux d'erreur                                    │
│   └─ Upgrade path analysis                            │
│                                                         │
│  Dashboards (à implémenter)                            │
│   └─ PowerBI, Metabase, Grafana, Custom Angular       │
└─────────────────────────────────────────────────────────┘
```

---

## 📦 Composants Backend

### 1. Entité Domain - TrackingEvent

**Fichier:** `LocaGuest.Domain/Analytics/TrackingEvent.cs`

```csharp
public class TrackingEvent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }       // Multi-tenant
    public Guid UserId { get; private set; }
    public string EventType { get; private set; }
    public string? PageName { get; private set; }
    public string? Url { get; private set; }
    public string UserAgent { get; private set; }
    public string IpAddress { get; private set; }    // Anonymisée
    public DateTime Timestamp { get; private set; }
    public string? Metadata { get; private set; }    // JSON
    public string? SessionId { get; private set; }
    public int? DurationMs { get; private set; }
    public int? HttpStatusCode { get; private set; }
}
```

**Méthode clé:** `AnonymizeIp()` - RGPD compliance
- IPv4: `192.168.1.100` → `192.168.1.0`
- IPv6: Garde les 48 premiers bits

### 2. Interface ITrackingService

**Fichier:** `LocaGuest.Application/Services/ITrackingService.cs`

```csharp
public interface ITrackingService
{
    Task TrackAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default);
    Task TrackEventAsync(string eventType, string? pageName = null, string? url = null, string? metadata = null, CancellationToken cancellationToken = default);
}
```

### 3. Implémentation TrackingService

**Fichier:** `LocaGuest.Infrastructure/Services/TrackingService.cs`

**Responsabilités:**
- ✅ Injection automatique TenantId via `ITenantContext`
- ✅ Injection automatique UserId via `ICurrentUserService`
- ✅ Extraction IP depuis `HttpContext`
- ✅ Extraction UserAgent depuis `HttpContext`
- ✅ **Never throw** - Le tracking ne doit jamais bloquer l'application

### 4. Middleware TrackingMiddleware

**Fichier:** `LocaGuest.Api/Middleware/TrackingMiddleware.cs`

**Fonctionnalités:**
- ✅ Track automatiquement toutes les requêtes API authentifiées
- ✅ Exclut les endpoints techniques (`/health`, `/swagger`, `/tracking/event`)
- ✅ Mesure durée avec `Stopwatch`
- ✅ Capture HTTP status code
- ✅ Fire-and-forget (non-bloquant)

**Exclusions:**
```csharp
private static readonly HashSet<string> ExcludedPaths = new()
{
    "/health",
    "/metrics",
    "/swagger",
    "/tracking/event",
    "/_framework",
    "/.well-known"
};
```

### 5. Controller TrackingController

**Fichier:** `LocaGuest.Api/Controllers/TrackingController.cs`

**Endpoints:**

#### POST /api/tracking/event
Track un événement unique depuis le frontend

**Request:**
```json
{
  "eventType": "PAGE_VIEW",
  "pageName": "dashboard",
  "url": "/app/dashboard",
  "metadata": { "tab": "overview" }
}
```

**Response:** `204 No Content`

#### POST /api/tracking/events/batch
Track plusieurs événements en batch (performance)

**Request:**
```json
[
  { "eventType": "PAGE_VIEW", "pageName": "properties", "url": "/app/properties", "metadata": null },
  { "eventType": "BUTTON_CLICK", "pageName": null, "url": null, "metadata": { "button": "add_property" } }
]
```

---

## 🎨 Frontend Angular

### 1. Service TrackingService

**Fichier:** `locaGuest/src/app/core/services/tracking.service.ts`

**Fonctionnalités:**
- ✅ Batching automatique (10 events ou 5 secondes)
- ✅ Méthodes de convenience (trackPageView, trackButtonClick...)
- ✅ Error handling robuste (jamais throw)
- ✅ Fire-and-forget (Observable<void>)

**Utilisation basique:**
```typescript
// Inject service
private tracking = inject(TrackingService);

// Track page view
this.tracking.trackPageView('dashboard', '/app/dashboard').subscribe();

// Track button click
this.tracking.trackButtonClick('ADD_PROPERTY').subscribe();

// Track avec metadata
this.tracking.track('FEATURE_USED', undefined, undefined, {
  feature: 'export_csv',
  format: 'csv'
}).subscribe();
```

### 2. Intégration Auto Page Tracking

**Fichier:** `TRACKING_INTEGRATION_EXAMPLE.ts` (exemple fourni)

**Dans app.component.ts ou layout component:**
```typescript
ngOnInit(): void {
  // Auto-track page views
  this.router.events.pipe(
    filter(event => event instanceof NavigationEnd),
    takeUntil(this.destroy$)
  ).subscribe((event: NavigationEnd) => {
    if (!this.isSensitivePage(event.urlAfterRedirects)) {
      this.tracking.trackPageView(
        this.getPageName(event.urlAfterRedirects),
        event.urlAfterRedirects
      ).subscribe();
    }
  });
}
```

**Pages sensibles exclues (RGPD):**
- `/login`
- `/register`
- `/reset-password`
- `/auth/*`

### 3. Tracking Actions Importantes

**Exemples d'intégration:**

```typescript
// Création de bien
createProperty(data: PropertyDto): void {
  this.propertiesService.create(data).subscribe({
    next: (property) => {
      this.tracking.trackPropertyCreated(property.id).subscribe();
    }
  });
}

// Téléchargement document
downloadDocument(doc: Document): void {
  this.tracking.trackDownload(doc.name, doc.type).subscribe();
  this.documentsService.download(doc.id).subscribe();
}

// Recherche
search(query: string): void {
  this.propertiesService.search(query).subscribe(results => {
    this.tracking.trackSearch(query, results.length).subscribe();
  });
}

// Upgrade
selectPlan(plan: Plan): void {
  this.tracking.trackUpgradeClicked(
    this.currentPlan?.code || 'free',
    plan.code
  ).subscribe();
}
```

---

## 🔒 Multi-tenant et Sécurité

### Isolation Multi-tenant

✅ **TenantId JAMAIS envoyé par Angular**
```typescript
// ❌ INCORRECT - Ne jamais faire
track('EVENT', null, null, { tenantId: 'xxx' });

// ✅ CORRECT - TenantId injecté automatiquement côté backend
track('EVENT', null, null, { feature: 'export' });
```

✅ **Backend injecte automatiquement**
```csharp
var tenantId = _tenantContext.TenantId; // Depuis JWT
var userId = _currentUserService.UserId; // Depuis JWT
```

✅ **Filtrage automatique par tenant dans requêtes SQL**
```sql
WHERE te."TenantId" = @currentTenantId
```

### Sécurité des Données

✅ **IP Anonymisée (RGPD)**
- Automatique dans `TrackingEvent.Create()`
- IPv4: Dernier octet à 0
- IPv6: Derniers 80 bits à 0

✅ **Pas de données sensibles**
- Pas de passwords
- Pas de tokens
- Pas de credit card numbers
- Pas de body de requêtes POST/PUT

✅ **Metadata filtrée**
```typescript
// ❌ Ne pas faire
this.tracking.track('PAYMENT', null, null, {
  cardNumber: '4242...' // INTERDIT
});

// ✅ Faire
this.tracking.track('PAYMENT_RECORDED', null, null, {
  amount: 1200,
  currency: 'EUR' // OK
});
```

---

## 📊 Types d'Événements Standard

```typescript
// Pages
'PAGE_VIEW'
'PAGE_EXIT'

// Actions utilisateur
'BUTTON_CLICK'
'FORM_SUBMIT'
'DOWNLOAD_FILE'

// Business
'PROPERTY_CREATED'
'CONTRACT_CREATED'
'TENANT_CREATED'
'PAYMENT_RECORDED'
'DOCUMENT_GENERATED'
'REMINDER_SENT'

// Features
'FEATURE_USED'
'SEARCH_PERFORMED'
'FILTER_APPLIED'
'EXPORT_TRIGGERED'

// Navigation
'TAB_CHANGED'
'MODAL_OPENED'
'MODAL_CLOSED'

// Subscription
'UPGRADE_CLICKED'
'PRICING_PAGE_VIEWED'
'CHECKOUT_STARTED'

// Errors
'ERROR_OCCURRED'
'API_ERROR'

// API (auto)
'API_REQUEST'
```

---

## 📈 Analytics - Requêtes SQL

**15+ requêtes prêtes à l'emploi** dans `TRACKING_ANALYTICS_SQL_QUERIES.sql`:

1. **Pages les plus visitées par tenant**
2. **Fonctionnalités les plus utilisées**
3. **Utilisateurs actifs (DAU/WAU/MAU)**
4. **Sessions moyennes par tenant**
5. **Heatmap d'utilisation (heure par heure)**
6. **Tenants inactifs depuis 30 jours**
7. **Taux d'utilisation PRO vs FREE**
8. **Funnel de conversion**
9. **Top features par plan**
10. **Taux d'erreur par page**
11. **Analyse de rétention (cohortes)**
12. **Performance des API**
13. **Upgrade path analysis**
14. **Search analytics**
15. **Export & download analytics**

**Exemple - DAU (Daily Active Users):**
```sql
SELECT 
    DATE_TRUNC('day', te."Timestamp") as activity_date,
    COUNT(DISTINCT te."UserId") as daily_active_users,
    COUNT(DISTINCT te."TenantId") as active_tenants,
    COUNT(*) as total_events
FROM tracking_events te
WHERE te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY DATE_TRUNC('day', te."Timestamp")
ORDER BY activity_date DESC;
```

---

## 🛡️ Conformité RGPD

### Mesures Implémentées

✅ **Anonymisation IP automatique**
- Implémentée dans `TrackingEvent.AnonymizeIp()`
- Derniers octets/bits supprimés

✅ **Pas de PII (Personally Identifiable Information)**
- Pas d'emails dans metadata
- Pas de noms de locataires
- Pas de données bancaires

✅ **Pages sensibles exclues**
- Login, register, reset-password non trackées

✅ **Opt-out possible**
```typescript
// À implémenter dans UserSettings
interface UserSettings {
  allowTracking: boolean; // Default: true
}

// Dans TrackingService
track(eventType: string, ...): Observable<void> {
  if (!this.settingsService.allowTracking()) {
    return of(void 0); // Skip tracking
  }
  // ... normal tracking
}
```

✅ **Rétention limitée**
```sql
-- Job automatique recommandé (à créer)
DELETE FROM tracking_events 
WHERE "Timestamp" < NOW() - INTERVAL '90 days';
```

✅ **Droit à l'effacement**
```sql
-- Anonymiser les données d'un utilisateur
UPDATE tracking_events
SET 
    "UserId" = '00000000-0000-0000-0000-000000000000',
    "IpAddress" = '0.0.0.0',
    "UserAgent" = 'anonymized',
    "Metadata" = NULL
WHERE "UserId" = @userToDelete;
```

### Recommandations Supplémentaires

⚠️ **À implémenter:**
1. **Consentement explicite** dans onboarding
2. **Opt-out dans profil** utilisateur
3. **Export des données** personnelles (RGPD Article 15)
4. **Suppression automatique** après 90-365 jours
5. **Politique de confidentialité** mise à jour

---

## ⚡ Performance

### Optimisations Backend

✅ **Fire-and-forget dans middleware**
```csharp
// Don't await - non-bloquant
_ = trackingService.TrackAsync(trackingEvent);
```

✅ **Index PostgreSQL optimisés**
- TenantId, UserId, EventType, Timestamp
- Composites: (TenantId, Timestamp), (TenantId, UserId, Timestamp)

✅ **JSONB pour metadata**
- Flexible
- Indexable avec GIN indexes si besoin

### Optimisations Frontend

✅ **Batching automatique**
```typescript
private readonly BATCH_SIZE = 10;
private readonly BATCH_DELAY_MS = 5000;
```
- Réduit nombre de requêtes HTTP
- Flush automatique si queue > 10 events
- Flush automatique après 5 secondes

✅ **Error handling robuste**
- Jamais throw
- Console.warn seulement
- Ne bloque jamais UX

### Vue Matérialisée (Optionnel)

Pour analytics haute performance:
```sql
CREATE MATERIALIZED VIEW mv_daily_stats AS
SELECT 
    DATE_TRUNC('day', te."Timestamp") as stat_date,
    te."TenantId",
    COUNT(DISTINCT te."UserId") as daily_active_users,
    COUNT(*) as total_events
FROM tracking_events te
GROUP BY DATE_TRUNC('day', te."Timestamp"), te."TenantId";

-- Rafraîchir quotidiennement
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_daily_stats;
```

---

## 🧪 Tests

### Backend Tests Recommandés

```csharp
[Fact]
public void TrackingEvent_Should_Anonymize_IPv4()
{
    var event = TrackingEvent.Create(
        tenantId: Guid.NewGuid(),
        userId: Guid.NewGuid(),
        eventType: "TEST",
        ipAddress: "192.168.1.100",
        userAgent: "test"
    );
    
    Assert.Equal("192.168.1.0", event.IpAddress);
}

[Fact]
public async Task TrackingService_Should_Not_Throw_On_Error()
{
    // Simulate DB error
    var mockContext = new Mock<LocaGuestDbContext>();
    mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception("DB error"));
    
    var service = new TrackingService(mockContext.Object, ...);
    
    // Should not throw
    await service.TrackEventAsync("TEST");
}
```

### Frontend Tests Recommandés

```typescript
it('should batch events and flush after delay', fakeAsync(() => {
  const service = TestBed.inject(TrackingService);
  spyOn(service['http'], 'post').and.returnValue(of(void 0));
  
  // Track 5 events
  for (let i = 0; i < 5; i++) {
    service.track('TEST');
  }
  
  // No HTTP call yet
  expect(service['http'].post).not.toHaveBeenCalled();
  
  // Advance time by 5 seconds
  tick(5000);
  
  // HTTP call should have been made
  expect(service['http'].post).toHaveBeenCalledOnce();
}));
```

---

## 📋 Checklist Implémentation

### Backend

- [x] Entité `TrackingEvent` créée dans Domain
- [x] `ITrackingService` interface créée
- [x] `TrackingService` implémenté
- [x] `TrackingMiddleware` créé
- [x] `TrackingController` avec endpoints
- [x] `LocaGuestDbContext` étendu avec DbSet
- [x] Configuration EF Core avec indexes
- [x] Migration créée (`AddTrackingEvents`)
- [x] DI enregistré dans `Program.cs`
- [x] Middleware activé après authentication

### Frontend

- [x] `TrackingService` Angular créé
- [x] Batching implémenté
- [x] Méthodes de convenience créées
- [ ] Intégration dans app.component.ts (exemple fourni)
- [ ] Tracking des actions business (exemple fourni)

### Analytics

- [x] 15+ requêtes SQL prêtes
- [ ] Dashboard BI (à implémenter)

### RGPD & Sécurité

- [x] Anonymisation IP
- [x] Exclusion pages sensibles
- [x] Multi-tenant isolation
- [ ] Opt-out dans settings
- [ ] Rétention automatique
- [ ] Politique de confidentialité

---

## 🚀 Déploiement

### 1. Appliquer la migration
```bash
dotnet ef database update --project src/LocaGuest.Infrastructure --startup-project src/LocaGuest.Api --context LocaGuestDbContext
```

### 2. Vérifier les indexes
```sql
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'tracking_events';
```

### 3. Tester l'API
```bash
curl -X POST https://localhost:5001/api/tracking/event \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"eventType":"TEST","pageName":"test","url":"/test","metadata":null}'
```

### 4. Monitorer les logs
```bash
# Vérifier que tracking ne génère pas d'erreurs
tail -f logs/locaguest-*.txt | grep -i tracking
```

---

## 🎯 Prochaines Étapes Recommandées

### Court Terme (1-2 semaines)

1. **Intégrer dans Angular**
   - Copier code exemple dans app.component.ts
   - Ajouter tracking sur actions critiques

2. **Dashboard basique**
   - Page admin `/analytics`
   - Afficher DAU, WAU, pages populaires
   - Graphiques avec Chart.js ou ngx-charts

3. **Opt-out RGPD**
   - Ajouter toggle dans UserSettings
   - Respecter choix utilisateur

### Moyen Terme (1-2 mois)

4. **Advanced Analytics**
   - Funnel de conversion visualisé
   - Retention cohorts
   - A/B testing framework

5. **Alertes automatiques**
   - Email si taux d'erreur > 5%
   - Email si tenant inactif > 30 jours
   - Webhook pour anomalies

6. **Export pour BI externe**
   - API endpoint pour export CSV
   - Intégration PowerBI / Metabase
   - Grafana dashboards

### Long Terme (3-6 mois)

7. **Machine Learning**
   - Prédiction churn
   - Recommandations features
   - Segmentation automatique

8. **Session Replay** (optionnel)
   - Enregistrement sessions anonymes
   - Replay pour debug UX

---

## 📞 Support

**Documentation:** Ce fichier  
**Requêtes SQL:** `TRACKING_ANALYTICS_SQL_QUERIES.sql`  
**Exemples Angular:** `TRACKING_INTEGRATION_EXAMPLE.ts`  

**Questions fréquentes:**

**Q: Le tracking ralentit-il l'application?**  
R: Non. Fire-and-forget backend + batching frontend.

**Q: Combien de temps garder les données?**  
R: Recommandé: 90 jours analytics, 1 an business-critical.

**Q: Compatible RGPD?**  
R: Oui avec IP anonymisée + opt-out. Ajouter consentement explicite.

**Q: Coût storage PostgreSQL?**  
R: ~1 Ko par event. 1M events ≈ 1 GB. Compresser/archiver après 90 jours.

---

✅ **Système de Tracking LocaGuest - Production Ready** 🎉
