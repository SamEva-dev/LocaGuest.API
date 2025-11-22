using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(LocaGuestDbContext context)
    {
        // Seed Plans first (always, even if other data exists)
        await PlanSeeder.SeedPlansAsync(context);
        Console.WriteLine("Subscription plans seeded");
        
        // Vérifier si déjà seedé
        if (await context.Properties.AnyAsync()) 
        {
            Console.WriteLine("Database already seeded, skipping...");
            return;
        }

        Console.WriteLine("Seeding database...");

        // Créer les biens
        var properties = new List<Property>
        {
            CreateProperty("T3 Centre Ville", "12 rue de la République", "Lyon", PropertyType.Apartment, 850, 3, 1, PropertyStatus.Occupied),
            CreateProperty("Studio Quartier Gare", "45 avenue Jean Jaurès", "Lyon", PropertyType.Studio, 450, 1, 1, PropertyStatus.Occupied),
            CreateProperty("T2 Quartier Nord", "8 rue des Lilas", "Lyon", PropertyType.Apartment, 650, 2, 1, PropertyStatus.Occupied),
            CreateProperty("Maison Familiale", "23 chemin des Roses", "Villeurbanne", PropertyType.House, 1200, 4, 2, PropertyStatus.Vacant),
            CreateProperty("T4 Vue Parc", "56 boulevard Vivier Merle", "Lyon", PropertyType.Apartment, 950, 4, 2, PropertyStatus.Occupied),
            CreateProperty("Studio Étudiant", "78 rue Pasteur", "Lyon", PropertyType.Studio, 420, 1, 1, PropertyStatus.Occupied),
            CreateProperty("T3 Moderne", "15 place Bellecour", "Lyon", PropertyType.Apartment, 1100, 3, 1, PropertyStatus.Occupied),
            CreateProperty("Duplex Centre", "34 rue de la Charité", "Lyon", PropertyType.Duplex, 1350, 4, 2, PropertyStatus.Occupied),
            CreateProperty("T2 Lumineux", "67 cours Gambetta", "Lyon", PropertyType.Apartment, 700, 2, 1, PropertyStatus.Occupied),
            CreateProperty("Studio Cosy", "89 rue Garibaldi", "Lyon", PropertyType.Studio, 480, 1, 1, PropertyStatus.Vacant),
            CreateProperty("T3 Terrasse", "12 montée de la Grande Côte", "Lyon", PropertyType.Apartment, 920, 3, 1, PropertyStatus.Occupied),
            CreateProperty("Maison Jardin", "5 allée des Cerisiers", "Caluire", PropertyType.House, 1450, 5, 2, PropertyStatus.Occupied)
        };

        // Clear domain events before saving (seeding context)
        foreach (var prop in properties)
        {
            prop.ClearDomainEvents();
        }

        context.Properties.AddRange(properties);
        await context.SaveChangesAsync();

        // Créer les locataires
        var tenants = new List<Tenant>
        {
            CreateTenant("Marie Dupont", "marie.dupont@email.com", "0612345678"),
            CreateTenant("Pierre Martin", "pierre.martin@email.com", "0623456789"),
            CreateTenant("Sophie Bernard", "sophie.bernard@email.com", "0634567890"),
            CreateTenant("Jean Dubois", "jean.dubois@email.com", "0645678901"),
            CreateTenant("Isabelle Leroy", "isabelle.leroy@email.com", "0656789012"),
            CreateTenant("Thomas Moreau", "thomas.moreau@email.com", "0667890123"),
            CreateTenant("Julie Simon", "julie.simon@email.com", "0678901234"),
            CreateTenant("Nicolas Laurent", "nicolas.laurent@email.com", "0689012345"),
            CreateTenant("Émilie Blanc", "emilie.blanc@email.com", "0690123456"),
            CreateTenant("Alexandre Garnier", "alex.garnier@email.com", "0601234567"),
            CreateTenant("Camille Faure", "camille.faure@email.com", "0612340987")
        };

        // Clear domain events
        foreach (var tenant in tenants)
        {
            tenant.ClearDomainEvents();
        }

        context.Tenants.AddRange(tenants);
        await context.SaveChangesAsync();

        // Créer les contrats (associer propriétés occupées avec locataires)
        var contracts = new List<Contract>
        {
            CreateContract(properties[0].Id, tenants[0].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-18), DateTime.UtcNow.AddMonths(6), 850),
            CreateContract(properties[1].Id, tenants[1].Id, ContractType.Furnished, DateTime.UtcNow.AddMonths(-8), DateTime.UtcNow.AddMonths(4), 450),
            CreateContract(properties[2].Id, tenants[2].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-24), DateTime.UtcNow.AddMonths(-3), 650), // Expire bientôt
            CreateContract(properties[4].Id, tenants[3].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow.AddMonths(12), 950),
            CreateContract(properties[5].Id, tenants[4].Id, ContractType.Furnished, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(6), 420),
            CreateContract(properties[6].Id, tenants[5].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-20), DateTime.UtcNow.AddMonths(4), 1100),
            CreateContract(properties[7].Id, tenants[6].Id, ContractType.Furnished, DateTime.UtcNow.AddMonths(-10), DateTime.UtcNow.AddMonths(14), 1350),
            CreateContract(properties[8].Id, tenants[7].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-15), DateTime.UtcNow.AddMonths(9), 700),
            CreateContract(properties[10].Id, tenants[8].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-9), DateTime.UtcNow.AddMonths(15), 920),
            CreateContract(properties[11].Id, tenants[9].Id, ContractType.Unfurnished, DateTime.UtcNow.AddMonths(-30), DateTime.UtcNow.AddMonths(6), 1450)
        };

        // Marquer le contrat qui expire dans moins de 3 mois
        contracts[2].MarkAsExpiring();

        // Clear domain events
        foreach (var contract in contracts)
        {
            contract.ClearDomainEvents();
        }

        context.Contracts.AddRange(contracts);
        await context.SaveChangesAsync();

        // ⭐ Créer les associations bidirectionnelles Tenant ↔ Property
        // Association basée sur les contrats créés
        tenants[0].AssociateToProperty(properties[0].Id, properties[0].Code);
        properties[0].AddTenant(tenants[0].Code);

        tenants[1].AssociateToProperty(properties[1].Id, properties[1].Code);
        properties[1].AddTenant(tenants[1].Code);

        tenants[2].AssociateToProperty(properties[2].Id, properties[2].Code);
        properties[2].AddTenant(tenants[2].Code);

        tenants[3].AssociateToProperty(properties[4].Id, properties[4].Code);
        properties[4].AddTenant(tenants[3].Code);

        tenants[4].AssociateToProperty(properties[5].Id, properties[5].Code);
        properties[5].AddTenant(tenants[4].Code);

        tenants[5].AssociateToProperty(properties[6].Id, properties[6].Code);
        properties[6].AddTenant(tenants[5].Code);

        tenants[6].AssociateToProperty(properties[7].Id, properties[7].Code);
        properties[7].AddTenant(tenants[6].Code);

        tenants[7].AssociateToProperty(properties[8].Id, properties[8].Code);
        properties[8].AddTenant(tenants[7].Code);

        tenants[8].AssociateToProperty(properties[10].Id, properties[10].Code);
        properties[10].AddTenant(tenants[8].Code);

        tenants[9].AssociateToProperty(properties[11].Id, properties[11].Code);
        properties[11].AddTenant(tenants[9].Code);

        // ⭐ Associer aussi le locataire 10 (Camille) au bien vacant (Maison Familiale)
        // Pour tester la génération de contrat sans contrat actif
        tenants[10].AssociateToProperty(properties[3].Id, properties[3].Code);
        properties[3].AddTenant(tenants[10].Code);

        await context.SaveChangesAsync();

        // Créer les paiements (12 derniers mois pour chaque contrat actif)
        var random = new Random(42);
        foreach (var contract in contracts)
        {
            var monthsBack = (int)(DateTime.UtcNow - contract.StartDate).TotalDays / 30;
            var paymentsToCreate = Math.Min(monthsBack, 12);

            for (int i = 0; i < paymentsToCreate; i++)
            {
                var paymentDate = DateTime.UtcNow.AddMonths(-i).AddDays(-random.Next(0, 5));
                var method = (PaymentMethod)random.Next(0, 4);
                
                contract.RecordPayment(contract.Rent, paymentDate, method);
            }

            // Simuler 1-2 paiements en retard sur certains contrats
            if (random.Next(0, 3) == 0)
            {
                var latePayment = contract.Payments.OrderByDescending(p => p.PaymentDate).First();
                contract.MarkPaymentAsLate(latePayment.Id);
            }
        }

        await context.SaveChangesAsync();
    }

    private static Property CreateProperty(
        string name,
        string address,
        string city,
        PropertyType type,
        decimal rent,
        int bedrooms,
        int bathrooms,
        PropertyStatus status)
    {
        var property = Property.Create(name, address, city, type, rent, bedrooms, bathrooms);
        property.SetStatus(status);
        return property;
    }

    private static Tenant CreateTenant(string fullName, string email, string phone)
    {
        var tenant = Tenant.Create(fullName, email, phone);
        tenant.SetMoveInDate(DateTime.UtcNow.AddMonths(-new Random().Next(6, 30)));
        return tenant;
    }

    private static Contract CreateContract(
        Guid propertyId,
        Guid tenantId,
        ContractType type,
        DateTime startDate,
        DateTime endDate,
        decimal rent)
    {
        return Contract.Create(propertyId, tenantId, type, startDate, endDate, rent, rent * 2);
    }
}
