using LocaGuest.Application.Services;
using LocaGuest.Application.Features.Analytics.Tracking.Queries.GetTrackingEvent;
using LocaGuest.Application.Features.Analytics.Tracking.Queries.GetTrackingEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

/// <summary>
/// Controller for tracking events from frontend
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly IMediator _mediator;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ITrackingService trackingService,
        IMediator mediator,
        ILogger<TrackingController> logger)
    {
        _trackingService = trackingService;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? sessionId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var query = new GetTrackingEventsQuery
        {
            Page = page,
            PageSize = pageSize,
            EventType = eventType,
            UserId = userId,
            SessionId = sessionId,
            CorrelationId = correlationId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };

        var result = await _mediator.Send(query);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTrackingEventQuery(id));
        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Track a custom event from frontend (Angular)
    /// </summary>
    /// <param name="dto">Event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NoContent if successful</returns>
    [HttpPost("event")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrackEvent(
        [FromBody] TrackingEventDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.EventType))
            {
                return BadRequest("EventType is required");
            }

            // Serialize metadata to JSON if provided
            string? metadataJson = null;
            if (dto.Metadata != null)
            {
                metadataJson = System.Text.Json.JsonSerializer.Serialize(dto.Metadata);
            }

            await _trackingService.TrackEventAsync(
                eventType: dto.EventType,
                pageName: dto.PageName,
                url: dto.Url,
                metadata: metadataJson,
                cancellationToken: cancellationToken
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking event: {EventType}", dto.EventType);
            // Return success even on error - tracking should not break user experience
            return NoContent();
        }
    }

    /// <summary>
    /// Batch track multiple events (for performance)
    /// </summary>
    /// <param name="dtos">Array of events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NoContent if successful</returns>
    [HttpPost("events/batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TrackEventsBatch(
        [FromBody] TrackingEventDto[] dtos,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var dto in dtos)
            {
                if (!string.IsNullOrWhiteSpace(dto.EventType))
                {
                    string? metadataJson = dto.Metadata != null
                        ? System.Text.Json.JsonSerializer.Serialize(dto.Metadata)
                        : null;

                    await _trackingService.TrackEventAsync(
                        eventType: dto.EventType,
                        pageName: dto.PageName,
                        url: dto.Url,
                        metadata: metadataJson,
                        cancellationToken: cancellationToken
                    );
                }
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking batch events");
            return NoContent(); // Don't break user experience
        }
    }
}

/// <summary>
/// DTO for tracking event from frontend
/// </summary>
public record TrackingEventDto(
    string EventType,
    string? PageName,
    string? Url,
    object? Metadata
);
