using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? siteId)
    {
        var ctx = UserContext.FromClaims(User);
        var summary = await _dashboard.GetSummaryAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId);
        if (summary == null) return Forbid();
        return Ok(summary);
    }
}
