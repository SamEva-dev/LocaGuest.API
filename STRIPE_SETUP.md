# Configuration Stripe - Guide Complet

## 1. Créer un compte Stripe

1. Aller sur [https://stripe.com](https://stripe.com)
2. Créer un compte (mode Test par défaut)
3. Récupérer les clés API:
   - **Dashboard** → **Developers** → **API keys**
   - `Publishable key`: `pk_test_...`
   - `Secret key`: `sk_test_...`

## 2. Créer les Products et Prices

### Via Stripe Dashboard

1. **Dashboard** → **Products** → **Add Product**

### Plan Pro (19€/mois)
```
Name: LocaGuest Pro
Description: Pour les investisseurs actifs et professionnels
Price: 19 EUR/month (recurring)
Price ID: price_xxx (noter pour le seeder)
```

### Plan Business (49€/mois)
```
Name: LocaGuest Business
Description: Pour les équipes et agences immobilières
Price: 49 EUR/month (recurring)
Price ID: price_yyy
```

### Plan Pro Annuel (182.40€/an)
```
Name: LocaGuest Pro (Annual)
Price: 182.40 EUR/year (recurring)
Price ID: price_zzz
```

### Plan Business Annuel (470.40€/an)
```
Name: LocaGuest Business (Annual)
Price: 470.40 EUR/year (recurring)
Price ID: price_aaa
```

### Via Stripe CLI (Alternative)

```bash
# Pro Monthly
stripe products create \
  --name="LocaGuest Pro" \
  --description="Pour les investisseurs actifs"

stripe prices create \
  --product=prod_xxx \
  --unit-amount=1900 \
  --currency=eur \
  --recurring[interval]=month

# Business Monthly
stripe products create \
  --name="LocaGuest Business" \
  --description="Pour les équipes"

stripe prices create \
  --product=prod_yyy \
  --unit-amount=4900 \
  --currency=eur \
  --recurring[interval]=month

# Pro Annual (-20%)
stripe prices create \
  --product=prod_xxx \
  --unit-amount=18240 \
  --currency=eur \
  --recurring[interval]=year

# Business Annual (-20%)
stripe prices create \
  --product=prod_yyy \
  --unit-amount=47040 \
  --currency=eur \
  --recurring[interval]=year
```

## 3. Mettre à jour les Price IDs dans le Seeder

Après création des prices, copier les IDs et exécuter:

```sql
-- Pro Plan
UPDATE plans 
SET stripe_monthly_price_id = 'price_xxx',
    stripe_annual_price_id = 'price_zzz'
WHERE code = 'pro';

-- Business Plan
UPDATE plans 
SET stripe_monthly_price_id = 'price_yyy',
    stripe_annual_price_id = 'price_aaa'
WHERE code = 'business';
```

## 4. Configurer les Webhooks

### En Local (Développement)

1. Installer Stripe CLI:
```bash
# Windows (Chocolatey)
choco install stripe-cli

# Mac (Homebrew)
brew install stripe/stripe-cli/stripe

# Ou télécharger depuis https://github.com/stripe/stripe-cli/releases
```

2. Se connecter:
```bash
stripe login
```

3. Forwarder les webhooks vers l'API locale:
```bash
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
```

4. Copier le webhook secret (`whsec_...`) et le mettre dans `appsettings.json`

### En Production

1. **Dashboard** → **Developers** → **Webhooks** → **Add endpoint**
2. URL: `https://yourdomain.com/api/webhooks/stripe`
3. Événements à écouter:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
4. Copier le webhook secret

## 5. Tester les Webhooks

### Test avec Stripe CLI

```bash
# Simuler un checkout réussi
stripe trigger checkout.session.completed

# Simuler une souscription créée
stripe trigger customer.subscription.created

# Simuler un paiement échoué
stripe trigger invoice.payment_failed
```

### Test Manuel

1. Utiliser les cartes de test Stripe:
   - `4242 4242 4242 4242` - Paiement réussi
   - `4000 0000 0000 0002` - Carte refusée
   - `4000 0000 0000 9995` - Insufficient funds

2. Date d'expiration: N'importe quelle date future
3. CVC: N'importe quels 3 chiffres
4. ZIP: N'importe quel code postal

## 6. Mettre à jour appsettings.json

```json
{
  "Stripe": {
    "SecretKey": "sk_test_VOTRE_CLE_SECRETE",
    "PublishableKey": "pk_test_VOTRE_CLE_PUBLIQUE",
    "WebhookSecret": "whsec_VOTRE_WEBHOOK_SECRET",
    "SuccessUrl": "http://localhost:4201/subscription/success",
    "CancelUrl": "http://localhost:4201/pricing"
  }
}
```

## 7. Variables d'environnement (Production)

Ne JAMAIS commiter les clés. Utiliser des variables d'environnement:

```bash
export STRIPE_SECRET_KEY="sk_live_..."
export STRIPE_PUBLISHABLE_KEY="pk_live_..."
export STRIPE_WEBHOOK_SECRET="whsec_..."
```

## 8. Vérification

### Backend
```bash
curl -X GET https://localhost:5001/api/subscriptions/plans
```

### Webhooks
```bash
stripe listen --print-secret
stripe trigger checkout.session.completed
```

### Logs
Vérifier les logs dans `logs/locaguest-*.txt`

## Ressources

- [Stripe Dashboard](https://dashboard.stripe.com)
- [Documentation API](https://stripe.com/docs/api)
- [Stripe CLI](https://stripe.com/docs/stripe-cli)
- [Cartes de test](https://stripe.com/docs/testing)
- [Webhooks Guide](https://stripe.com/docs/webhooks)
