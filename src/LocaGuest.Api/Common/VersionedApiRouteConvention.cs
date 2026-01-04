using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace LocaGuest.Api.Common;

public sealed class VersionedApiRouteConvention : IApplicationModelConvention
{
    private readonly string _versionSegment;

    public VersionedApiRouteConvention(string versionSegment)
    {
        _versionSegment = versionSegment.Trim('/');
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            ApplyToSelectors(controller.Selectors);

            foreach (var action in controller.Actions)
            {
                ApplyToSelectors(action.Selectors);
            }
        }
    }

    private void ApplyToSelectors(IList<SelectorModel> selectors)
    {
        var toAdd = new List<SelectorModel>();

        foreach (var selector in selectors)
        {
            var routeModel = selector.AttributeRouteModel;
            var template = routeModel?.Template;
            if (string.IsNullOrWhiteSpace(template))
                continue;

            if (!template.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                continue;

            if (template.StartsWith($"api/{_versionSegment}/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(template, $"api/{_versionSegment}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var prefixedTemplate = template.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
                ? $"api/{_versionSegment}/{template.Substring(4)}"
                : $"api/{_versionSegment}/{template}";

            var newSelector = new SelectorModel(selector)
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = prefixedTemplate
                }
            };

            toAdd.Add(newSelector);
        }

        foreach (var s in toAdd)
            selectors.Add(s);
    }
}
