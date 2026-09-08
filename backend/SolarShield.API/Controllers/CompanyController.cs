using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly CompanyService _company;

    public CompanyController(CompanyService company) => _company = company;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var ctx = UserContext.FromClaims(User);
        var data = await _company.GetDashboardAsync(ctx.UserId, ctx.Role, ctx.OrganizationId);
        return data == null ? Forbid() : Ok(data);
    }

    [HttpGet("operations-efficiency")]
    public async Task<IActionResult> GetOperationsEfficiency()
    {
        var ctx = UserContext.FromClaims(User);
        var data = await _company.GetOperationsEfficiencyAsync(ctx.UserId, ctx.Role, ctx.OrganizationId);
        return data == null ? Forbid() : Ok(data);
    }

    [HttpGet("energy-loss")]
    public async Task<IActionResult> GetEnergyLoss([FromQuery] int? siteId)
    {
        var ctx = UserContext.FromClaims(User);
        var data = await _company.GetEnergyLossAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId);
        return data == null ? NotFound() : Ok(data);
    }
}
