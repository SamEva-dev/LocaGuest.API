using LocaGuest.Application.Features.Team.Commands.AcceptInvitation;
using LocaGuest.Application.Features.Team.Commands.InviteTeamMember;
using LocaGuest.Application.Features.Team.Commands.RemoveTeamMember;
using LocaGuest.Application.Features.Team.Commands.UpdateTeamMemberRole;
using LocaGuest.Application.Features.Team.Queries.GetTeamMembers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocaGuest.Api.Authorization;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TeamController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TeamController> _logger;

    public TeamController(IMediator mediator, ILogger<TeamController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Récupère la liste des membres de l'équipe
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.TeamRead)]
    public async Task<IActionResult> GetTeamMembers([FromQuery] bool activeOnly = true)
    {
        var query = new GetTeamMembersQuery { ActiveOnly = activeOnly };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Invite un nouveau membre dans l'équipe
    /// </summary>
    [HttpPost("invite")]
    [Authorize(Policy = Permissions.TeamManage)]
    public async Task<IActionResult> InviteTeamMember([FromBody] InviteTeamMemberCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (string.Equals(
                result.ErrorMessage,
                "Invitations are managed by AuthGate. Use AuthGate /api/auth/invite to invite collaborators.",
                StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Modifie le rôle d'un membre de l'équipe
    /// </summary>
    [HttpPut("{teamMemberId}/role")]
    [Authorize(Policy = Permissions.TeamManage)]
    public async Task<IActionResult> UpdateTeamMemberRole(
        Guid teamMemberId, 
        [FromBody] UpdateTeamMemberRoleRequest request)
    {
        var command = new UpdateTeamMemberRoleCommand
        {
            TeamMemberId = teamMemberId,
            NewRole = request.NewRole
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Supprime un membre de l'équipe
    /// </summary>
    [HttpDelete("{teamMemberId}")]
    [Authorize(Policy = Permissions.TeamManage)]
    public async Task<IActionResult> RemoveTeamMember(Guid teamMemberId)
    {
        var command = new RemoveTeamMemberCommand { TeamMemberId = teamMemberId };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Accepter une invitation d'équipe (public, pas d'auth requise)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var command = new AcceptInvitationCommand { Token = request.Token };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
}

public record UpdateTeamMemberRoleRequest(string NewRole);
public record AcceptInvitationRequest(string Token);
