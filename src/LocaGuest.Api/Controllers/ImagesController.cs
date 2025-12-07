using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.PropertyImages.Commands.DeleteImage;
using LocaGuest.Application.Features.PropertyImages.Commands.UploadImages;
using LocaGuest.Application.Features.PropertyImages.Queries.GetImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(IMediator mediator, ILogger<ImagesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Upload une ou plusieurs images pour une propriété
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max par requête
    public async Task<IActionResult> UploadImages(
        [FromForm] Guid propertyId,
        [FromForm] List<IFormFile> files,
        [FromForm] string? category = "other")
    {
        // Convertir IFormFile en UploadedFileInfo
        var uploadedFiles = files.Select(f => new UploadedFileInfo
        {
            FileName = f.FileName,
            ContentType = f.ContentType,
            Length = f.Length,
            Stream = f.OpenReadStream()
        }).ToList();

        var command = new UploadImagesCommand
        {
            PropertyId = propertyId,
            Files = uploadedFiles,
            Category = category ?? "other"
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupère une image par son ID
    /// </summary>
    [HttpGet("{imageId:guid}")]
    [ResponseCache(Duration = 86400)] // Cache 24h
    public async Task<IActionResult> GetImage(Guid imageId)
    {
        var query = new GetImageQuery { ImageId = imageId };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(result.ErrorMessage);
        }

        return File(result.Data!.FileBytes, result.Data.ContentType);
    }

    /// <summary>
    /// Supprime une image
    /// </summary>
    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        var command = new DeleteImageCommand { ImageId = imageId };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return NoContent();
    }
}
