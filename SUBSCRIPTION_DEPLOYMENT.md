# 🚀 Guide de Déploiement - Système d'Abonnement

## 📋 Checklist Pré-Déploiement

### ✅ Backend (.NET)
- [ ] Migration EF Core appliquée en production
- [ ] Plans seedés dans la base
- [ ] Variables d'environnement configurées
- [ ] Stripe configuré (mode Live)
- [ ] Tests d'intégration passés
- [ ] Logs configurés

### ✅ Frontend (Angular)
- [ ] Build de production généré
- [ ] Environment variables configurées
- [ ] Service initialisé dans AppComponent
- [ ] Routes protégées par guards
- [ ] Tests e2e passés

### ✅ Stripe
- [ ] Products créés en mode Live
- [ ] Prices configurés
- [ ] Webhook endpoint configuré
- [ ] Webhook secret sauvegardé
- [ ] Cartes de test validées

---

## 🗄️ Base de Données

### **1. Appliquer les migrations**

```bash
# Développement
cd src/LocaGuest.Api
dotnet ef database update

# Production (avec connection string)
dotnet ef database update --connection "Host=prod-db;Database=locaguest;..."
```

### **2. Seed initial des plans**

Le seeding est automatique au démarrage si les plans n'existent pas.

Vérifier avec:
```sql
SELECT * FROM plans ORDER BY monthly_price;
```

Si besoin de re-seed manuellement:
```sql
DELETE FROM usage_aggregates;
DELETE FROM usage_events;
DELETE FROM subscriptions;
DELETE FROM plans;
```

Puis redémarrer l'application.

### **3. Backup recommandé**

```bash
# PostgreSQL
pg_dump -h localhost -U postgres -d locaguest -F c -f backup_$(date +%Y%m%d).dump

# Restore
pg_restore -h localhost -U postgres -d locaguest backup_20251111.dump
```

---

## ⚙️ Configuration Production

### **1. Variables d'environnement Backend**

Créer un fichier `.env` ou configurer dans le hosting:

```bash
# Database
ConnectionStrings__Default="Host=prod-db;Database=locaguest;Username=xxx;Password=xxx"

# Stripe
Stripe__SecretKey="sk_live_XXX"
Stripe__PublishableKey="pk_live_XXX"
Stripe__WebhookSecret="whsec_XXX"
Stripe__SuccessUrl="https://app.locaguest.com/subscription/success"
Stripe__CancelUrl="https://app.locaguest.com/pricing"

# JWT
Jwt__Issuer="AuthGate"
Jwt__Audience="AuthGate"

# AuthGate
AuthGate__Url="https://authgate.prod.com"
```

### **2. Variables d'environnement Frontend**

`src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  BASE_LOCAGUEST_API: 'https://api.locaguest.com',
  STRIPE_PUBLISHABLE_KEY: 'pk_live_XXX'
};
```

### **3. Configuration Stripe en Production**

#### **Créer les Products**

1. Aller sur https://dashboard.stripe.com/products
2. Basculer en mode **Live** (toggle en haut à droite)
3. Créer les products:

**Product 1: LocaGuest Pro**
```
Name: LocaGuest Pro
Description: Pour les investisseurs actifs
```

Créer 2 prices:
- Monthly: 19 EUR/month → `price_pro_monthly_live`
- Annual: 182.40 EUR/year → `price_pro_annual_live`

**Product 2: LocaGuest Business**
```
Name: LocaGuest Business
Description: Pour les équipes et agences
```

Créer 2 prices:
- Monthly: 49 EUR/month → `price_business_monthly_live`
- Annual: 470.40 EUR/year → `price_business_annual_live`

#### **Mettre à jour les Price IDs dans la base**

```sql
-- Pro Plan
UPDATE plans 
SET stripe_monthly_price_id = 'price_pro_monthly_live',
    stripe_annual_price_id = 'price_pro_annual_live'
WHERE code = 'pro';

-- Business Plan
UPDATE plans 
SET stripe_monthly_price_id = 'price_business_monthly_live',
    stripe_annual_price_id = 'price_business_annual_live'
WHERE code = 'business';
```

#### **Configurer le Webhook**

1. Dashboard Stripe → **Developers** → **Webhooks**
2. **Add endpoint**
3. URL: `https://api.locaguest.com/api/webhooks/stripe`
4. Événements à écouter:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
5. Copier le **Signing secret** (`whsec_...`)
6. L'ajouter dans les variables d'environnement

---

## 🐳 Docker

### **Dockerfile Backend**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/LocaGuest.Api/LocaGuest.Api.csproj", "src/LocaGuest.Api/"]
COPY ["src/LocaGuest.Application/LocaGuest.Application.csproj", "src/LocaGuest.Application/"]
COPY ["src/LocaGuest.Domain/LocaGuest.Domain.csproj", "src/LocaGuest.Domain/"]
COPY ["src/LocaGuest.Infrastructure/LocaGuest.Infrastructure.csproj", "src/LocaGuest.Infrastructure/"]
RUN dotnet restore "src/LocaGuest.Api/LocaGuest.Api.csproj"
COPY . .
WORKDIR "/src/src/LocaGuest.Api"
RUN dotnet build "LocaGuest.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LocaGuest.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocaGuest.Api.dll"]
```

### **docker-compose.yml**

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: locaguest
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ConnectionStrings__Default=Host=postgres;Database=locaguest;Username=postgres;Password=${DB_PASSWORD}
      - Stripe__SecretKey=${STRIPE_SECRET_KEY}
      - Stripe__WebhookSecret=${STRIPE_WEBHOOK_SECRET}
    ports:
      - "5001:80"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

### **Commandes Docker**

```bash
# Build
docker-compose build

# Run
docker-compose up -d

# Logs
docker-compose logs -f api

# Arrêt
docker-compose down
```

---

## ☁️ Déploiement Cloud

### **Option 1: Azure**

#### **Backend (App Service)**

```bash
# Créer un App Service
az webapp create --resource-group rg-locaguest --plan asp-locaguest --name api-locaguest --runtime "DOTNETCORE:9.0"

# Configurer les variables
az webapp config appsettings set --resource-group rg-locaguest --name api-locaguest --settings \
  Stripe__SecretKey="sk_live_XXX" \
  Stripe__WebhookSecret="whsec_XXX"

# Déployer
az webapp deployment source config-zip --resource-group rg-locaguest --name api-locaguest --src publish.zip
```

#### **Frontend (Static Web App)**

```bash
# Créer un Static Web App
az staticwebapp create --name app-locaguest --resource-group rg-locaguest --location "West Europe"

# Déployer
az staticwebapp deploy --name app-locaguest --resource-group rg-locaguest --app-location ./dist/locaguest
```

### **Option 2: AWS**

#### **Backend (Elastic Beanstalk)**

```bash
# Initialiser EB
eb init -p "64bit Amazon Linux 2023 v3.0.0 running .NET 9" locaguest-api

# Créer environnement
eb create locaguest-api-prod

# Configurer variables
eb setenv Stripe__SecretKey="sk_live_XXX" Stripe__WebhookSecret="whsec_XXX"

# Déployer
eb deploy
```

#### **Frontend (S3 + CloudFront)**

```bash
# Build
ng build --configuration production

# Upload vers S3
aws s3 sync ./dist/locaguest s3://locaguest-app --delete

# Invalider CloudFront cache
aws cloudfront create-invalidation --distribution-id EXXX --paths "/*"
```

### **Option 3: Vercel (Frontend) + Railway (Backend)**

#### **Frontend (Vercel)**

```bash
# Installer Vercel CLI
npm i -g vercel

# Déployer
cd locaGuest
vercel --prod
```

#### **Backend (Railway)**

1. Connecter repo GitHub à Railway
2. Créer nouveau service → Deploy from GitHub
3. Sélectionner LocaGuest.API
4. Ajouter PostgreSQL addon
5. Configurer variables d'environnement
6. Deploy automatique

---

## 📊 Monitoring

### **1. Application Insights (Azure)**

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  }
}
```

### **2. Serilog pour les logs**

Déjà configuré dans le projet. Logs écrits dans:
- Console (dev)
- Fichiers `logs/locaguest-*.txt`
- Application Insights (prod)

### **3. Healthcheck endpoint**

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default"))
    .AddUrlGroup(new Uri(builder.Configuration["Stripe:ApiUrl"]), "Stripe");

app.MapHealthChecks("/health");
```

### **4. Métriques à surveiller**

**Backend**:
- Nombre de webhooks Stripe reçus
- Taux d'erreur des webhooks
- Temps de réponse des endpoints subscription
- Nombre de quota exceeded (429)
- Nombre de feature forbidden (403)

**Base de données**:
- Nombre total de subscriptions par statut
- Nombre d'événements d'usage par jour
- Croissance MRR (Monthly Recurring Revenue)

Requête SQL pour MRR:
```sql
SELECT 
  DATE_TRUNC('month', current_period_start) as month,
  COUNT(*) as active_subscriptions,
  SUM(p.monthly_price) as mrr
FROM subscriptions s
JOIN plans p ON s.plan_id = p.id
WHERE s.status IN ('active', 'trialing')
GROUP BY month
ORDER BY month DESC;
```

---

## 🔐 Sécurité

### **1. Secrets Management**

**Azure Key Vault**:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential()
);
```

**AWS Secrets Manager**:
```bash
aws secretsmanager create-secret --name stripe-secret-key --secret-string "sk_live_XXX"
```

### **2. HTTPS uniquement**

```csharp
// Program.cs
app.UseHttpsRedirection();
app.UseHsts();
```

### **3. Rate Limiting**

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

app.UseRateLimiter();

// Sur endpoints critiques
[EnableRateLimiting("api")]
[HttpPost("checkout/create-session")]
```

### **4. CORS**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins("https://app.locaguest.com")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("Production");
```

---

## 🧪 Tests de Production

### **1. Test du Checkout Flow**

```bash
# 1. Créer session
curl -X POST https://api.locaguest.com/api/checkout/create-session \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"planId":"xxx","isAnnual":false}'

# 2. Vérifier que l'URL Stripe est retournée
# 3. Compléter le paiement avec carte de test
#    - 4242 4242 4242 4242 (succès)
#    - 4000 0000 0000 0002 (refusée)

# 4. Vérifier webhook reçu
curl https://api.locaguest.com/health

# 5. Vérifier subscription créée
curl https://api.locaguest.com/api/subscriptions/current \
  -H "Authorization: Bearer $TOKEN"
```

### **2. Test du Feature Gating**

```bash
# Avec plan Free (devrait échouer)
curl https://api.locaguest.com/api/analytics/advanced \
  -H "Authorization: Bearer $FREE_TOKEN"
# Attendu: 403 Forbidden

# Avec plan Business (devrait réussir)
curl https://api.locaguest.com/api/analytics/advanced \
  -H "Authorization: Bearer $BUSINESS_TOKEN"
# Attendu: 200 OK
```

### **3. Test du Quota**

```bash
# Créer 4 scénarios avec plan Free (limite: 3)
for i in {1..4}; do
  curl -X POST https://api.locaguest.com/api/scenarios \
    -H "Authorization: Bearer $FREE_TOKEN" \
    -d '{"name":"Scenario '$i'"}'
done
# Attendu: 3x 201 Created, 1x 429 Too Many Requests
```

---

## 🛠️ Maintenance

### **1. Rotation des secrets**

```bash
# Générer nouvelle clé Stripe
stripe keys create

# Update dans Key Vault / Secrets Manager
az keyvault secret set --vault-name kv-locaguest --name stripe-secret-key --value "sk_live_NEW"

# Redémarrer l'application
az webapp restart --name api-locaguest --resource-group rg-locaguest
```

### **2. Nettoyage des anciens events**

Script à exécuter mensuellement:

```sql
-- Supprimer events > 6 mois
DELETE FROM usage_events
WHERE timestamp < NOW() - INTERVAL '6 months';

-- Supprimer agrégats > 2 ans
DELETE FROM usage_aggregates
WHERE period_start < NOW() - INTERVAL '2 years';

-- Vacuum
VACUUM ANALYZE usage_events;
VACUUM ANALYZE usage_aggregates;
```

### **3. Monitoring des subscriptions expirées**

```sql
-- Subscriptions en past_due depuis > 7 jours
SELECT s.id, s.user_id, s.status, s.current_period_end, p.name as plan
FROM subscriptions s
JOIN plans p ON s.plan_id = p.id
WHERE s.status = 'past_due'
AND s.current_period_end < NOW() - INTERVAL '7 days';
```

Action: Envoyer email de relance ou downgrade vers Free.

### **4. Rapports mensuels**

```sql
-- Churn rate
WITH current_month AS (
  SELECT COUNT(*) as active
  FROM subscriptions
  WHERE status = 'active'
  AND DATE_TRUNC('month', current_period_start) = DATE_TRUNC('month', NOW())
),
previous_month AS (
  SELECT COUNT(*) as active
  FROM subscriptions
  WHERE status = 'canceled'
  AND DATE_TRUNC('month', canceled_at) = DATE_TRUNC('month', NOW() - INTERVAL '1 month')
)
SELECT 
  (previous_month.active::float / current_month.active::float) * 100 as churn_rate_percent
FROM current_month, previous_month;

-- MRR growth
WITH monthly_mrr AS (
  SELECT 
    DATE_TRUNC('month', current_period_start) as month,
    SUM(p.monthly_price) as mrr
  FROM subscriptions s
  JOIN plans p ON s.plan_id = p.id
  WHERE s.status IN ('active', 'trialing')
  GROUP BY month
)
SELECT 
  month,
  mrr,
  LAG(mrr) OVER (ORDER BY month) as previous_mrr,
  ((mrr - LAG(mrr) OVER (ORDER BY month)) / LAG(mrr) OVER (ORDER BY month)) * 100 as growth_percent
FROM monthly_mrr
ORDER BY month DESC
LIMIT 12;
```

---

## 📞 Support

### **Problèmes courants**

**Webhook non reçu**:
1. Vérifier Dashboard Stripe → Developers → Events
2. Vérifier logs backend
3. Tester manuellement avec `stripe trigger`

**Paiement échoué**:
1. Vérifier carte dans Dashboard Stripe
2. Email envoyé automatiquement par Stripe
3. Retry automatique après 3, 5, 7 jours

**Quota incorrect**:
1. Vérifier agrégats du mois
2. Recalculer si nécessaire
3. Forcer refresh côté frontend

---

## ✅ Checklist Post-Déploiement

- [ ] Backend accessible et healthy
- [ ] Frontend déployé et fonctionnel
- [ ] Database migrée avec plans seedés
- [ ] Stripe configuré (Products, Prices, Webhooks)
- [ ] Variables d'environnement en prod
- [ ] Tests manuels du checkout flow
- [ ] Tests feature gating
- [ ] Tests quota checking
- [ ] Monitoring activé
- [ ] Alertes configurées
- [ ] Backup automatique configuré
- [ ] Documentation accessible à l'équipe

---

**Système prêt pour la production ! 🚀**
