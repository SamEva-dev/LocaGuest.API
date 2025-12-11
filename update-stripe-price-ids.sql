-- =============================================
-- Script: Mettre à jour les Stripe Price IDs
-- Description: Exécuter ce script après avoir créé les Products et Prices dans Stripe Dashboard
-- =============================================

-- IMPORTANT: Remplacer les valeurs price_xxx par les vrais Price IDs de Stripe

-- Plan FREE (pas de price ID car gratuit)
UPDATE Plans 
SET StripeMonthlyPriceId = NULL,
    StripeAnnualPriceId = NULL,
    LastModifiedAt = datetime('now'),
    LastModifiedBy = 'Admin'
WHERE Code = 'free';

-- Plan PRO
UPDATE Plans 
SET StripeMonthlyPriceId = 'price_VOTRE_PRO_MONTHLY_PRICE_ID_ICI',  -- Remplacer par le vrai Price ID mensuel
    StripeAnnualPriceId = 'price_VOTRE_PRO_ANNUAL_PRICE_ID_ICI',   -- Remplacer par le vrai Price ID annuel
    LastModifiedAt = datetime('now'),
    LastModifiedBy = 'Admin'
WHERE Code = 'pro';

-- Plan BUSINESS
UPDATE Plans 
SET StripeMonthlyPriceId = 'price_VOTRE_BUSINESS_MONTHLY_PRICE_ID_ICI',
    StripeAnnualPriceId = 'price_VOTRE_BUSINESS_ANNUAL_PRICE_ID_ICI',
    LastModifiedAt = datetime('now'),
    LastModifiedBy = 'Admin'
WHERE Code = 'business';

-- Plan ENTERPRISE (sur devis, pas de price ID automatique)
UPDATE Plans 
SET StripeMonthlyPriceId = NULL,
    StripeAnnualPriceId = NULL,
    LastModifiedAt = datetime('now'),
    LastModifiedBy = 'Admin'
WHERE Code = 'enterprise';

-- Vérifier les mises à jour
SELECT Code, Name, MonthlyPrice, AnnualPrice, StripeMonthlyPriceId, StripeAnnualPriceId
FROM Plans
ORDER BY SortOrder;
