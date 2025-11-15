using LocaGuest.Application.Services;
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
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ITrackingService trackingService,
        ILogger<TrackingController> logger)
    {
        _trackingService = trackingService;
        _logger = logger;
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
