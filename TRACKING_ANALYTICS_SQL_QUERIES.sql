-- ====================================================================
-- TRACKING ANALYTICS - SQL QUERIES FOR BUSINESS INTELLIGENCE
-- LocaGuest - Product Analytics & User Behavior Tracking
-- ====================================================================

-- ====================================================================
-- 1. PAGES LES PLUS VISITÉES PAR TENANT
-- ====================================================================
SELECT 
    te."TenantId",
    te."PageName",
    COUNT(*) as visit_count,
    COUNT(DISTINCT te."UserId") as unique_users,
    DATE_TRUNC('day', te."Timestamp") as visit_date
FROM tracking_events te
WHERE 
    te."EventType" = 'PAGE_VIEW'
    AND te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY te."TenantId", te."PageName", DATE_TRUNC('day', te."Timestamp")
ORDER BY te."TenantId", visit_count DESC;

-- ====================================================================
-- 2. FONCTIONNALITÉS LES PLUS UTILISÉES
-- ====================================================================
SELECT 
    te."EventType",
    te."Metadata"->>'feature' as feature_name,
    COUNT(*) as usage_count,
    COUNT(DISTINCT te."TenantId") as tenant_count,
    COUNT(DISTINCT te."UserId") as user_count
FROM tracking_events te
WHERE 
    te."EventType" = 'FEATURE_USED'
    AND te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY te."EventType", te."Metadata"->>'feature'
ORDER BY usage_count DESC
LIMIT 20;

-- ====================================================================
-- 3. NOMBRE D'UTILISATEURS ACTIFS PAR JOUR/SEMAINE
-- ====================================================================
-- Par jour
SELECT 
    DATE_TRUNC('day', te."Timestamp") as activity_date,
    COUNT(DISTINCT te."UserId") as daily_active_users,
    COUNT(DISTINCT te."TenantId") as active_tenants,
    COUNT(*) as total_events
FROM tracking_events te
WHERE te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY DATE_TRUNC('day', te."Timestamp")
ORDER BY activity_date DESC;

-- Par semaine
SELECT 
    DATE_TRUNC('week', te."Timestamp") as activity_week,
    COUNT(DISTINCT te."UserId") as weekly_active_users,
    COUNT(DISTINCT te."TenantId") as active_tenants,
    COUNT(*) as total_events
FROM tracking_events te
WHERE te."Timestamp" >= NOW() - INTERVAL '90 days'
GROUP BY DATE_TRUNC('week', te."Timestamp")
ORDER BY activity_week DESC;

-- ====================================================================
-- 4. SESSIONS MOYENNES PAR TENANT
-- ====================================================================
WITH session_durations AS (
    SELECT 
        te."TenantId",
        te."SessionId",
        MIN(te."Timestamp") as session_start,
        MAX(te."Timestamp") as session_end,
        EXTRACT(EPOCH FROM (MAX(te."Timestamp") - MIN(te."Timestamp"))) / 60 as duration_minutes,
        COUNT(*) as event_count
    FROM tracking_events te
    WHERE 
        te."SessionId" IS NOT NULL
        AND te."Timestamp" >= NOW() - INTERVAL '30 days'
    GROUP BY te."TenantId", te."SessionId"
)
SELECT 
    sd."TenantId",
    COUNT(*) as total_sessions,
    AVG(sd.duration_minutes)::numeric(10,2) as avg_session_duration_min,
    AVG(sd.event_count)::numeric(10,2) as avg_events_per_session,
    MAX(sd.duration_minutes)::numeric(10,2) as max_session_duration_min
FROM session_durations sd
GROUP BY sd."TenantId"
ORDER BY avg_session_duration_min DESC;

-- ====================================================================
-- 5. HEATMAP D'UTILISATION (HEURE PAR HEURE)
-- ====================================================================
SELECT 
    EXTRACT(HOUR FROM te."Timestamp") as hour_of_day,
    EXTRACT(DOW FROM te."Timestamp") as day_of_week, -- 0=Sunday, 6=Saturday
    COUNT(*) as event_count,
    COUNT(DISTINCT te."UserId") as unique_users
FROM tracking_events te
WHERE te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY EXTRACT(HOUR FROM te."Timestamp"), EXTRACT(DOW FROM te."Timestamp")
ORDER BY day_of_week, hour_of_day;

-- ====================================================================
-- 6. TENANTS INACTIFS DEPUIS 30 JOURS
-- ====================================================================
WITH active_tenants AS (
    SELECT DISTINCT "TenantId"
    FROM tracking_events
    WHERE "Timestamp" >= NOW() - INTERVAL '30 days'
),
all_tenants AS (
    SELECT DISTINCT "TenantId"
    FROM tracking_events
)
SELECT 
    at."TenantId",
    MAX(te."Timestamp") as last_activity,
    EXTRACT(DAY FROM (NOW() - MAX(te."Timestamp"))) as days_since_last_activity
FROM all_tenants at
LEFT JOIN tracking_events te ON te."TenantId" = at."TenantId"
WHERE at."TenantId" NOT IN (SELECT "TenantId" FROM active_tenants)
GROUP BY at."TenantId"
ORDER BY days_since_last_activity DESC;

-- ====================================================================
-- 7. TAUX D'UTILISATION PLAN PRO VS FREE
-- ====================================================================
WITH tenant_plans AS (
    SELECT 
        s."TenantId",
        p."Code" as plan_code,
        p."Name" as plan_name
    FROM subscriptions s
    INNER JOIN plans p ON p."Id" = s."PlanId"
    WHERE s."Status" IN ('active', 'trialing')
),
tenant_activity AS (
    SELECT 
        te."TenantId",
        COUNT(*) as event_count,
        COUNT(DISTINCT te."UserId") as user_count,
        COUNT(DISTINCT DATE_TRUNC('day', te."Timestamp")) as active_days
    FROM tracking_events te
    WHERE te."Timestamp" >= NOW() - INTERVAL '30 days'
    GROUP BY te."TenantId"
)
SELECT 
    COALESCE(tp.plan_code, 'free') as plan,
    COUNT(DISTINCT ta."TenantId") as tenant_count,
    AVG(ta.event_count)::numeric(10,2) as avg_events_per_tenant,
    AVG(ta.user_count)::numeric(10,2) as avg_users_per_tenant,
    AVG(ta.active_days)::numeric(10,2) as avg_active_days_per_tenant
FROM tenant_activity ta
LEFT JOIN tenant_plans tp ON tp."TenantId" = ta."TenantId"
GROUP BY COALESCE(tp.plan_code, 'free')
ORDER BY plan;

-- ====================================================================
-- 8. FUNNEL DE CONVERSION (SIGNUP → PREMIER BIEN → PREMIER CONTRAT)
-- ====================================================================
WITH user_milestones AS (
    SELECT 
        te."TenantId",
        te."UserId",
        MIN(CASE WHEN te."EventType" = 'SESSION_START' THEN te."Timestamp" END) as signup_date,
        MIN(CASE WHEN te."EventType" = 'PROPERTY_CREATED' THEN te."Timestamp" END) as first_property_date,
        MIN(CASE WHEN te."EventType" = 'CONTRACT_CREATED' THEN te."Timestamp" END) as first_contract_date
    FROM tracking_events te
    WHERE te."Timestamp" >= NOW() - INTERVAL '90 days'
    GROUP BY te."TenantId", te."UserId"
)
SELECT 
    COUNT(*) as total_signups,
    COUNT(um.first_property_date) as users_created_property,
    COUNT(um.first_contract_date) as users_created_contract,
    (COUNT(um.first_property_date)::float / COUNT(*)::float * 100)::numeric(5,2) as property_conversion_rate,
    (COUNT(um.first_contract_date)::float / COUNT(*)::float * 100)::numeric(5,2) as contract_conversion_rate,
    AVG(EXTRACT(DAY FROM (um.first_property_date - um.signup_date)))::numeric(10,2) as avg_days_to_first_property,
    AVG(EXTRACT(DAY FROM (um.first_contract_date - um.signup_date)))::numeric(10,2) as avg_days_to_first_contract
FROM user_milestones um
WHERE um.signup_date IS NOT NULL;

-- ====================================================================
-- 9. TOP FEATURES PAR PLAN (PRO vs FREE)
-- ====================================================================
WITH tenant_plans AS (
    SELECT 
        s."TenantId",
        p."Code" as plan_code
    FROM subscriptions s
    INNER JOIN plans p ON p."Id" = s."PlanId"
    WHERE s."Status" IN ('active', 'trialing')
)
SELECT 
    COALESCE(tp.plan_code, 'free') as plan,
    te."Metadata"->>'feature' as feature_name,
    COUNT(*) as usage_count
FROM tracking_events te
LEFT JOIN tenant_plans tp ON tp."TenantId" = te."TenantId"
WHERE 
    te."EventType" = 'FEATURE_USED'
    AND te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY COALESCE(tp.plan_code, 'free'), te."Metadata"->>'feature'
ORDER BY plan, usage_count DESC;

-- ====================================================================
-- 10. TAUX D'ERREUR PAR PAGE/FEATURE
-- ====================================================================
WITH total_events AS (
    SELECT 
        te."PageName",
        COUNT(*) as total_count
    FROM tracking_events te
    WHERE 
        te."PageName" IS NOT NULL
        AND te."Timestamp" >= NOW() - INTERVAL '7 days'
    GROUP BY te."PageName"
),
error_events AS (
    SELECT 
        te."PageName",
        COUNT(*) as error_count
    FROM tracking_events te
    WHERE 
        te."EventType" = 'ERROR_OCCURRED'
        AND te."PageName" IS NOT NULL
        AND te."Timestamp" >= NOW() - INTERVAL '7 days'
    GROUP BY te."PageName"
)
SELECT 
    te.total_count."PageName",
    te.total_count,
    COALESCE(ee.error_count, 0) as error_count,
    (COALESCE(ee.error_count, 0)::float / te.total_count::float * 100)::numeric(5,2) as error_rate_percent
FROM total_events te
LEFT JOIN error_events ee ON ee."PageName" = te."PageName"
WHERE te.total_count > 10 -- Filter low traffic pages
ORDER BY error_rate_percent DESC, te.total_count DESC
LIMIT 20;

-- ====================================================================
-- 11. ANALYSE DE RÉTENTION (COHORTE)
-- ====================================================================
WITH user_cohorts AS (
    SELECT 
        te."UserId",
        DATE_TRUNC('month', MIN(te."Timestamp")) as cohort_month
    FROM tracking_events te
    GROUP BY te."UserId"
),
monthly_activity AS (
    SELECT 
        uc."UserId",
        uc.cohort_month,
        DATE_TRUNC('month', te."Timestamp") as activity_month,
        EXTRACT(MONTH FROM AGE(DATE_TRUNC('month', te."Timestamp"), uc.cohort_month))::int as months_since_signup
    FROM user_cohorts uc
    INNER JOIN tracking_events te ON te."UserId" = uc."UserId"
    GROUP BY uc."UserId", uc.cohort_month, DATE_TRUNC('month', te."Timestamp")
)
SELECT 
    ma.cohort_month,
    ma.months_since_signup,
    COUNT(DISTINCT ma."UserId") as active_users,
    (SELECT COUNT(DISTINCT "UserId") FROM user_cohorts WHERE cohort_month = ma.cohort_month) as cohort_size,
    (COUNT(DISTINCT ma."UserId")::float / 
     (SELECT COUNT(DISTINCT "UserId") FROM user_cohorts WHERE cohort_month = ma.cohort_month)::float * 100)::numeric(5,2) as retention_rate
FROM monthly_activity ma
WHERE ma.cohort_month >= NOW() - INTERVAL '12 months'
GROUP BY ma.cohort_month, ma.months_since_signup
ORDER BY ma.cohort_month DESC, ma.months_since_signup;

-- ====================================================================
-- 12. PERFORMANCE DES API PAR ENDPOINT
-- ====================================================================
SELECT 
    te."Url",
    COUNT(*) as request_count,
    AVG(te."DurationMs")::numeric(10,2) as avg_duration_ms,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY te."DurationMs")::numeric(10,2) as median_duration_ms,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY te."DurationMs")::numeric(10,2) as p95_duration_ms,
    MAX(te."DurationMs") as max_duration_ms,
    COUNT(CASE WHEN te."HttpStatusCode" >= 400 THEN 1 END) as error_count,
    (COUNT(CASE WHEN te."HttpStatusCode" >= 400 THEN 1 END)::float / COUNT(*)::float * 100)::numeric(5,2) as error_rate_percent
FROM tracking_events te
WHERE 
    te."EventType" = 'API_REQUEST'
    AND te."Timestamp" >= NOW() - INTERVAL '24 hours'
    AND te."Url" IS NOT NULL
GROUP BY te."Url"
HAVING COUNT(*) > 10
ORDER BY request_count DESC
LIMIT 30;

-- ====================================================================
-- 13. UPGRADE PATH ANALYSIS (FREE → PRO)
-- ====================================================================
SELECT 
    te."TenantId",
    MIN(CASE WHEN te."EventType" = 'PRICING_PAGE_VIEWED' THEN te."Timestamp" END) as first_pricing_view,
    COUNT(CASE WHEN te."EventType" = 'PRICING_PAGE_VIEWED' THEN 1 END) as pricing_page_views,
    MIN(CASE WHEN te."EventType" = 'UPGRADE_CLICKED' THEN te."Timestamp" END) as first_upgrade_click,
    COUNT(CASE WHEN te."EventType" = 'UPGRADE_CLICKED' THEN 1 END) as upgrade_clicks,
    MIN(CASE WHEN te."EventType" = 'CHECKOUT_STARTED' THEN te."Timestamp" END) as checkout_started,
    EXTRACT(DAY FROM (MIN(CASE WHEN te."EventType" = 'CHECKOUT_STARTED' THEN te."Timestamp" END) - 
                      MIN(CASE WHEN te."EventType" = 'PRICING_PAGE_VIEWED' THEN te."Timestamp" END)))::int as days_to_checkout
FROM tracking_events te
WHERE 
    te."EventType" IN ('PRICING_PAGE_VIEWED', 'UPGRADE_CLICKED', 'CHECKOUT_STARTED')
    AND te."Timestamp" >= NOW() - INTERVAL '90 days'
GROUP BY te."TenantId"
HAVING COUNT(CASE WHEN te."EventType" = 'PRICING_PAGE_VIEWED' THEN 1 END) > 0
ORDER BY checkout_started DESC NULLS LAST;

-- ====================================================================
-- 14. SEARCH ANALYTICS
-- ====================================================================
SELECT 
    te."Metadata"->>'query' as search_query,
    COUNT(*) as search_count,
    COUNT(DISTINCT te."UserId") as unique_searchers,
    AVG((te."Metadata"->>'resultsCount')::int)::numeric(10,2) as avg_results_count,
    COUNT(CASE WHEN (te."Metadata"->>'resultsCount')::int = 0 THEN 1 END) as zero_result_count,
    (COUNT(CASE WHEN (te."Metadata"->>'resultsCount')::int = 0 THEN 1 END)::float / COUNT(*)::float * 100)::numeric(5,2) as zero_result_rate
FROM tracking_events te
WHERE 
    te."EventType" = 'SEARCH_PERFORMED'
    AND te."Timestamp" >= NOW() - INTERVAL '30 days'
    AND te."Metadata"->>'query' IS NOT NULL
GROUP BY te."Metadata"->>'query'
ORDER BY search_count DESC
LIMIT 50;

-- ====================================================================
-- 15. EXPORT & DOWNLOAD ANALYTICS
-- ====================================================================
SELECT 
    DATE_TRUNC('day', te."Timestamp") as download_date,
    te."Metadata"->>'fileType' as file_type,
    COUNT(*) as download_count,
    COUNT(DISTINCT te."UserId") as unique_downloaders,
    COUNT(DISTINCT te."TenantId") as unique_tenants
FROM tracking_events te
WHERE 
    te."EventType" = 'DOWNLOAD_FILE'
    AND te."Timestamp" >= NOW() - INTERVAL '30 days'
GROUP BY DATE_TRUNC('day', te."Timestamp"), te."Metadata"->>'fileType'
ORDER BY download_date DESC, download_count DESC;

-- ====================================================================
-- INDEXES RECOMMANDÉS POUR PERFORMANCE
-- ====================================================================
-- Déjà créés dans la migration EF Core:
-- CREATE INDEX idx_tracking_events_tenant_id ON tracking_events ("TenantId");
-- CREATE INDEX idx_tracking_events_user_id ON tracking_events ("UserId");
-- CREATE INDEX idx_tracking_events_event_type ON tracking_events ("EventType");
-- CREATE INDEX idx_tracking_events_timestamp ON tracking_events ("Timestamp");
-- CREATE INDEX idx_tracking_events_tenant_timestamp ON tracking_events ("TenantId", "Timestamp");
-- CREATE INDEX idx_tracking_events_tenant_user_timestamp ON tracking_events ("TenantId", "UserId", "Timestamp");
-- CREATE INDEX idx_tracking_events_event_timestamp ON tracking_events ("EventType", "Timestamp");

-- ====================================================================
-- VUES MATÉRIALISÉES RECOMMANDÉES (OPTIONNEL - PERFORMANCE)
-- ====================================================================

-- Vue matérialisée pour stats quotidiennes
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_daily_stats AS
SELECT 
    DATE_TRUNC('day', te."Timestamp") as stat_date,
    te."TenantId",
    COUNT(DISTINCT te."UserId") as daily_active_users,
    COUNT(*) as total_events,
    COUNT(CASE WHEN te."EventType" = 'PAGE_VIEW' THEN 1 END) as page_views,
    COUNT(CASE WHEN te."EventType" = 'FEATURE_USED' THEN 1 END) as features_used
FROM tracking_events te
GROUP BY DATE_TRUNC('day', te."Timestamp"), te."TenantId";

CREATE UNIQUE INDEX ON mv_daily_stats (stat_date, "TenantId");

-- Rafraîchir quotidiennement
-- REFRESH MATERIALIZED VIEW CONCURRENTLY mv_daily_stats;
