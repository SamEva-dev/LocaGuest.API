# LocaGuest API - Guide de démarrage

## Prérequis

- .NET 9 SDK
- PostgreSQL 15+ (installé et en cours d'exécution)
- AuthGate API en cours d'exécution sur https://localhost:8081

## Configuration PostgreSQL

1. Créer la base de données:
```sql
CREATE DATABASE locaguest;
```

2. Créer l'utilisateur (optionnel, si différent de postgres):
```sql
CREATE USER locaguest_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE locaguest TO locaguest_user;
```

3. Mettre à jour la connection string dans `appsettings.json`:
```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=locaguest;Username=postgres;Password=your_password"
}
```

## Configuration JWT

Dans `appsettings.json`, configurer les paramètres JWT pour correspondre à AuthGate:

```json
"Jwt": {
  "SecretKey": "LA_MEME_CLE_QUE_AUTHGATE_MINIMUM_32_CARACTERES",
  "Issuer": "AuthGate",
  "Audience": "LocaGuest"
}
```

**Important**: La `SecretKey` doit être identique à celle utilisée par AuthGate.

## Initialisation de la base de données

### Méthode 1: Script PowerShell (recommandé)
```powershell
.\scripts\init-db.ps1
```

### Méthode 2: Commandes manuelles
```bash
cd src/LocaGuest.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../LocaGuest.Api/LocaGuest.Api.csproj
dotnet ef database update --startup-project ../LocaGuest.Api/LocaGuest.Api.csproj
```

## Lancement de l'API

1. Depuis la racine du projet:
```bash
cd src/LocaGuest.Api
dotnet run
```

2. L'API sera disponible sur: **https://localhost:5001**

3. Swagger UI: **https://localhost:5001/swagger**

## Test des endpoints

### 1. Obtenir un token JWT depuis AuthGate

```bash
curl -X POST https://localhost:8081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@authgate.com",
    "password": "Admin@123"
  }'
```

### 2. Tester le Dashboard

```bash
curl -X GET https://localhost:5001/api/dashboard/summary \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Structure du projet

```
LocaGuest.Api/
├── src/
│   ├── LocaGuest.Domain/         # Entités, agrégats, events, exceptions
│   ├── LocaGuest.Application/    # Use cases, DTOs, handlers
│   ├── LocaGuest.Infrastructure/ # EF Core, Postgres, Outbox
│   └── LocaGuest.Api/            # Controllers, Middleware, SignalR
└── scripts/                      # Scripts d'initialisation
```

## Endpoints disponibles

### Dashboard
- `GET /api/dashboard/summary` - Statistiques générales
- `GET /api/dashboard/activities?limit=20` - Activités récentes
- `GET /api/dashboard/deadlines` - Échéances
- `GET /api/dashboard/charts/occupancy?year=2025` - Graphique occupation
- `GET /api/dashboard/charts/revenue?year=2025` - Graphique revenus

### Health
- `GET /health` - État de santé de l'API

### SignalR
- Hub: `/hubs/notifications` - Notifications en temps réel

## CORS

L'API autorise les requêtes depuis `http://localhost:4200` (Angular frontend).

## Logs

Les logs sont enregistrés dans:
- Console (temps réel)
- Fichiers: `logs/locaguest-YYYYMMDD.txt` (rolling daily)

## Prochaines étapes

1. Implémenter les endpoints Properties, Tenants, Relations
2. Ajouter le seeder de données de démonstration
3. Implémenter l'Outbox pattern pour les Domain Events
4. Ajouter les projections Dashboard (tables matérialisées)
5. Connecter le frontend Angular

## Dépannage

### Erreur de connexion PostgreSQL
- Vérifier que PostgreSQL est en cours d'exécution
- Vérifier les credentials dans appsettings.json
- Tester la connexion: `psql -U postgres -h localhost`

### Erreur JWT 401 Unauthorized
- Vérifier que la SecretKey est identique à AuthGate
- Vérifier que le token n'est pas expiré
- Vérifier le format: `Authorization: Bearer <token>`

### Erreur CORS
- Vérifier que le frontend tourne sur http://localhost:4200
- Vérifier la policy CORS dans Program.cs
