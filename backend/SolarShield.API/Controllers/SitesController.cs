using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarShield.API.Extensions;
using SolarShield.API.Services;
using SolarShield.Core.DTOs;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SitesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public SitesController(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ctx = UserContext.FromClaims(User);
        var sites = await _access.GetAccessibleSitesAsync(ctx.UserId, ctx.Role, ctx.OrganizationId);
        return Ok(sites);
    }

    [HttpGet("{id}/zones")]
    public async Task<IActionResult> GetZones(int id)
    {
        var ctx = UserContext.FromClaims(User);
        if (!await _access.CanAccessSiteAsync(ctx.UserId, ctx.Role, ctx.OrganizationId, id))
            return Forbid();

        var zones = await _db.Zones
            .Where(z => z.SiteId == id)
            .Select(z => new { z.Id, z.Name, z.StringId, z.Latitude, z.Longitude, z.PanelCount })
            .ToListAsync();
        return Ok(zones);
    }
}
