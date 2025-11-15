using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using LocaGuest.Application.Common.Interfaces;
using System.Security.Claims;

namespace LocaGuest.Api.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresQuotaAttribute : TypeFilterAttribute
{
    public RequiresQuotaAttribute(string dimension) : base(typeof(RequiresQuotaFilter))
    {
        Arguments = new object[] { dimension };
    }
}

public class RequiresQuotaFilter : IAsyncActionFilter
{
    private readonly string _dimension;
    private readonly ISubscriptionService _subscriptionService;
    
    public RequiresQuotaFilter(string dimension, ISubscriptionService subscriptionService)
    {
        _dimension = dimension;
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
        
        var hasQuota = await _subscriptionService.CheckQuotaAsync(userId, _dimension);
        
        if (!hasQuota)
        {
            var currentUsage = await _subscriptionService.GetUsageAsync(userId, _dimension);
            
            context.Result = new ObjectResult(new
            {
                error = "quota_exceeded",
                message = $"You have reached your quota limit for '{_dimension}'",
                dimension = _dimension,
                current_usage = currentUsage
            })
            {
                StatusCode = 429
            };
            return;
        }
        
        await next();
    }
}
