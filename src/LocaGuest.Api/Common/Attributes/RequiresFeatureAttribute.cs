using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using LocaGuest.Application.Common.Interfaces;
using System.Security.Claims;

namespace LocaGuest.Api.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresFeatureAttribute : TypeFilterAttribute
{
    public RequiresFeatureAttribute(string featureName) : base(typeof(RequiresFeatureFilter))
    {
        Arguments = new object[] { featureName };
    }
}

public class RequiresFeatureFilter : IAsyncActionFilter
{
    private readonly string _featureName;
    private readonly ISubscriptionService _subscriptionService;
    
    public RequiresFeatureFilter(string featureName, ISubscriptionService subscriptionService)
    {
        _featureName = featureName;
        _subscriptionService = subscriptionService;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var hasAccess = await _subscriptionService.CanAccessFeatureAsync(userId, _featureName);
        
        if (!hasAccess)
        {
            context.Result = new ObjectResult(new
            {
                error = "feature_not_available",
                message = $"This feature requires an upgrade to access '{_featureName}'",
                feature = _featureName
            })
            {
                StatusCode = 403
            };
            return;
        }
        
        await next();
    }
}
