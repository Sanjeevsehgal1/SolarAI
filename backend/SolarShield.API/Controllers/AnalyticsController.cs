using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;
using SolarShield.Core.DTOs;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analytics;

    public AnalyticsController(AnalyticsService analytics) => _analytics = analytics;

    [HttpGet("{siteId}")]
    public async Task<IActionResult> GetSummary(int siteId, [FromQuery] int days = 30)
    {
        var ctx = UserContext.FromClaims(User);
        var summary = await _analytics.GetSummaryAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId, days);
        return summary == null ? Forbid() : Ok(summary);
    }
}
