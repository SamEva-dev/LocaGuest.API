-- Script de vérification après migration AddCodeColumns
-- À exécuter avec DB Browser for SQLite ou VS Code SQLite extension

-- 1. Vérifier structure des tables
PRAGMA table_info(tenants);
PRAGMA table_info(properties);
PRAGMA table_info(contracts);
PRAGMA table_info(payments);
PRAGMA table_info(rentability_scenarios);

-- 2. Compter les enregistrements existants
SELECT 'Tenants' as Table_Name, COUNT(*) as Count FROM tenants
UNION ALL
SELECT 'Properties', COUNT(*) FROM properties
UNION ALL
SELECT 'Contracts', COUNT(*) FROM contracts
UNION ALL
SELECT 'Payments', COUNT(*) FROM payments
UNION ALL
SELECT 'Scenarios', COUNT(*) FROM rentability_scenarios;

-- 3. Voir les derniers locataires créés (avec code)
SELECT 
    Id,
    Code,
    FirstName || ' ' || LastName as FullName,
    Email,
    Status,
    datetime(CreatedAtUtc, 'localtime') as Created
FROM tenants 
ORDER BY CreatedAtUtc DESC 
LIMIT 10;

-- 4. Voir les derniers biens créés (avec code)
SELECT 
    Id,
    Code,
    Name,
    Address,
    Rent,
    Status,
    datetime(CreatedAtUtc, 'localtime') as Created
FROM properties 
ORDER BY CreatedAtUtc DESC 
LIMIT 10;

-- 5. Vérifier les séquences de numérotation
SELECT 
    TenantId,
    EntityPrefix,
    LastValue,
    datetime(UpdatedAtUtc, 'localtime') as LastUpdated
FROM TenantSequences
ORDER BY EntityPrefix;

-- 6. Vérifier les codes vides (ne devrait rien retourner après test)
SELECT 'Tenants with empty Code' as Issue, COUNT(*) as Count 
FROM tenants WHERE Code = '' OR Code IS NULL
UNION ALL
SELECT 'Properties with empty Code', COUNT(*) 
FROM properties WHERE Code = '' OR Code IS NULL;

-- 7. Exemples de requêtes utiles

-- Trouver locataire par code
-- SELECT * FROM tenants WHERE Code = 'T0001-L0001';

-- Trouver bien par code  
-- SELECT * FROM properties WHERE Code = 'T0001-APP0001';

-- Lister tous les codes d'un tenant
-- SELECT 'Tenant' as Type, Code FROM tenants WHERE TenantId = '...'
-- UNION ALL
-- SELECT 'Property', Code FROM properties WHERE TenantId = '...'
-- UNION ALL
-- SELECT 'Contract', Code FROM contracts WHERE TenantId = '...';
