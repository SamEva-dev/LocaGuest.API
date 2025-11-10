using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Queries.GetDocumentTemplates;

public class GetDocumentTemplatesQueryHandler : IRequestHandler<GetDocumentTemplatesQuery, Result<List<DocumentTemplateDto>>>
{
    private readonly ILogger<GetDocumentTemplatesQueryHandler> _logger;

    public GetDocumentTemplatesQueryHandler(ILogger<GetDocumentTemplatesQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result<List<DocumentTemplateDto>>> Handle(GetDocumentTemplatesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Templates prédéfinis
            var templates = new List<DocumentTemplateDto>
            {
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Bail de location vide",
                    Description = "Contrat de location pour logement non meublé",
                    Category = "Contrats",
                    UsageCount = 45,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Bail de location meublé",
                    Description = "Contrat de location pour logement meublé",
                    Category = "Contrats",
                    UsageCount = 23,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "État des lieux d'entrée",
                    Description = "Document d'inventaire à l'entrée du locataire",
                    Category = "États des lieux",
                    UsageCount = 38,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "État des lieux de sortie",
                    Description = "Document d'inventaire à la sortie du locataire",
                    Category = "États des lieux",
                    UsageCount = 15,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Quittance de loyer",
                    Description = "Reçu de paiement mensuel",
                    Category = "Comptabilité",
                    UsageCount = 156,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Congé pour vente",
                    Description = "Notification de congé pour vendre le bien",
                    Category = "Résiliation",
                    UsageCount = 6,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Augmentation de loyer",
                    Description = "Notification d'augmentation de loyer",
                    Category = "Modifications",
                    UsageCount = 12,
                    CreatedAt = DateTime.UtcNow
                },
                new DocumentTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Autorisation de travaux",
                    Description = "Demande d'autorisation pour réaliser des travaux",
                    Category = "Travaux",
                    UsageCount = 6,
                    CreatedAt = DateTime.UtcNow
                }
            };

            return await Task.FromResult(Result.Success(templates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document templates");
            return Result.Failure<List<DocumentTemplateDto>>("Error retrieving document templates");
        }
    }
}
