-- Script pour supprimer toutes les tables de la base Locaguest
-- À exécuter dans pgAdmin ou psql

\c Locaguest;

DO $$ DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
    END LOOP;
END $$;

-- Vérifier qu'il ne reste plus de tables
\dt
