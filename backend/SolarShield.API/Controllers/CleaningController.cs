using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CleaningController : ControllerBase
{
    private readonly CleaningService _cleaning;

    public CleaningController(CleaningService cleaning) => _cleaning = cleaning;

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] int? siteId)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _cleaning.GetSchedulesAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }

    [HttpGet("soiling/{siteId}")]
    public async Task<IActionResult> GetSoiling(int siteId)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _cleaning.GetSoilingIndexAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }

    [HttpPost("recalculate/{siteId}")]
    public async Task<IActionResult> Recalculate(int siteId)
    {
        var ctx = UserContext.FromClaims(User);
        await _cleaning.RecalculateSoilingAsync(siteId);
        return Ok(await _cleaning.GetSoilingIndexAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }

    [HttpPut("schedules/{id}/complete")]
    public async Task<IActionResult> MarkComplete(int id, [FromQuery] string? photoUrl, [FromQuery] string? notes)
    {
        var ok = await _cleaning.MarkCompletedAsync(id, photoUrl, notes);
        return ok ? Ok() : NotFound();
    }
}
