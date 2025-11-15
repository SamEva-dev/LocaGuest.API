using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Stripe.Checkout;
using LocaGuest.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        IConfiguration configuration,
        ILocaGuestDbContext context,
        ILogger<CheckoutController> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
        
        // Configure Stripe
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    /// <summary>
    /// Crée une session Checkout Stripe pour un plan
    /// </summary>
    [HttpPost("create-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
    {
        var userId = GetUserId();
        var user = User;

        try
        {
            // Récupérer le plan
            var plan = await _context.Plans
                .FirstOrDefaultAsync(p => p.Id == request.PlanId);

            if (plan == null)
            {
                return NotFound(new { error = "Plan not found" });
            }

            // Déterminer le price ID selon la période
            var priceId = request.IsAnnual 
                ? plan.StripeAnnualPriceId 
                : plan.StripeMonthlyPriceId;

            if (string.IsNullOrEmpty(priceId))
            {
                return BadRequest(new { error = "Stripe price not configured for this plan" });
            }

            var email = user.FindFirst(ClaimTypes.Email)?.Value;

            // Créer la session Checkout
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = _configuration["Stripe:SuccessUrl"] + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _configuration["Stripe:CancelUrl"],
                CustomerEmail = email,
                ClientReferenceId = userId.ToString(),
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "plan_id", plan.Id.ToString() },
                        { "plan_code", plan.Code }
                    },
                    TrialPeriodDays = 14, // 14 jours d'essai gratuit
                },
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() },
                    { "plan_id", plan.Id.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Checkout session created: {SessionId} for user {UserId} and plan {PlanCode}",
                session.Id, userId, plan.Code);

            return Ok(new
            {
                sessionId = session.Id,
                url = session.Url
            });
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe error creating checkout session");
            return StatusCode(500, new { error = e.Message });
        }
    }

    /// <summary>
    /// Récupère les détails d'une session après paiement
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            return Ok(new
            {
                status = session.PaymentStatus,
                customerEmail = session.CustomerEmail,
                subscriptionId = session.SubscriptionId
            });
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Error retrieving session");
            return StatusCode(500, new { error = e.Message });
        }
    }

    /// <summary>
    /// Crée un portail de gestion de l'abonnement
    /// </summary>
    [HttpPost("create-portal-session")]
    public async Task<IActionResult> CreatePortalSession()
    {
        var userId = GetUserId();

        try
        {
            // Récupérer l'abonnement actif de l'utilisateur
            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
            {
                return BadRequest(new { error = "No active subscription found" });
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = subscription.StripeCustomerId,
                ReturnUrl = _configuration["Stripe:CancelUrl"], // Return to pricing page
            };

            var service = new Stripe.BillingPortal.SessionService();
            var portalSession = await service.CreateAsync(options);

            return Ok(new { url = portalSession.Url });
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Error creating portal session");
            return StatusCode(500, new { error = e.Message });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
}

public record CreateCheckoutRequest(Guid PlanId, bool IsAnnual);
