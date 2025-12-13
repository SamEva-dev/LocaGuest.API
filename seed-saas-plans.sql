-- SQL pour insérer les 4 plans SaaS dans PostgreSQL
-- À ajouter dans la migration SeedSaaSPlans après sa création

-- FREE Plan
INSERT INTO plans ("Id", "Code", "Name", "Description", "MonthlyPrice", "AnnualPrice", "MaxProperties", "MaxUsers", "MaxDocuments", "HasReporting", "HasApi", "HasPrioritySupport", "IsActive", "IsPublic", "CreatedAtUtc")
VALUES (
    gen_random_uuid(),
    'free',
    'Free',
    'Plan gratuit pour découvrir LocaGuest',
    0,
    0,
    5,
    1,
    50,
    false,
    false,
    false,
    true,
    true,
    NOW()
);

-- PRO Plan
INSERT INTO plans ("Id", "Code", "Name", "Description", "MonthlyPrice", "AnnualPrice", "MaxProperties", "MaxUsers", "MaxDocuments", "HasReporting", "HasApi", "HasPrioritySupport", "IsActive", "IsPublic", "CreatedAtUtc")
VALUES (
    gen_random_uuid(),
    'pro',
    'Pro',
    'Plan professionnel pour les particuliers et petites agences',
    29,
    290,
    25,
    3,
    500,
    true,
    false,
    false,
    true,
    true,
    NOW()
);

-- BUSINESS Plan
INSERT INTO plans ("Id", "Code", "Name", "Description", "MonthlyPrice", "AnnualPrice", "MaxProperties", "MaxUsers", "MaxDocuments", "HasReporting", "HasApi", "HasPrioritySupport", "IsActive", "IsPublic", "CreatedAtUtc")
VALUES (
    gen_random_uuid(),
    'business',
    'Business',
    'Plan business pour les agences immobilières',
    79,
    790,
    100,
    10,
    2000,
    true,
    true,
    false,
    true,
    true,
    NOW()
);

-- ENTERPRISE Plan
INSERT INTO plans ("Id", "Code", "Name", "Description", "MonthlyPrice", "AnnualPrice", "MaxProperties", "MaxUsers", "MaxDocuments", "HasReporting", "HasApi", "HasPrioritySupport", "IsActive", "IsPublic", "CreatedAtUtc")
VALUES (
    gen_random_uuid(),
    'enterprise',
    'Enterprise',
    'Plan entreprise sur devis pour les grandes agences',
    0,
    0,
    999999,
    999999,
    999999,
    true,
    true,
    true,
    true,
    false,
    NOW()
);
