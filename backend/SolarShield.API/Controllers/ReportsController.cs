using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportsService _reports;

    public ReportsController(ReportsService reports) => _reports = reports;

    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] int? siteId)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _reports.GetReportsAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }
}
