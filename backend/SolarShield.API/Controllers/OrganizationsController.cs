using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;
using SolarShield.Core.DTOs;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _organizations;

    public OrganizationsController(OrganizationService organizations) => _organizations = organizations;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _organizations.GetAllAsync(ctx.Role, ctx.OrganizationId));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetPlatformStats()
    {
        var ctx = UserContext.FromClaims(User);
        var stats = await _organizations.GetPlatformStatsAsync(ctx.Role);
        return stats == null ? Forbid() : Ok(stats);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ctx = UserContext.FromClaims(User);
        var org = await _organizations.GetByIdAsync(id, ctx.Role, ctx.OrganizationId);
        return org == null ? NotFound() : Ok(org);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        var org = await _organizations.CreateAsync(request, ctx.Role);
        return org == null ? Forbid() : Ok(org);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrganizationRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        var org = await _organizations.UpdateAsync(id, request, ctx.Role, ctx.OrganizationId);
        return org == null ? NotFound() : Ok(org);
    }
}
