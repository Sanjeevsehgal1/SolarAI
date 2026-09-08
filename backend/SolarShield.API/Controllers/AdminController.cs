using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;
using SolarShield.Core.DTOs;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AdminService _admin;
    private readonly AccessControlService _access;

    public AdminController(AdminService admin, AccessControlService access)
    {
        _admin = admin;
        _access = access;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int? organizationId)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _admin.GetUsersAsync(ctx.Role, ctx.OrganizationId, organizationId));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        var user = await _admin.CreateUserAsync(request, ctx.Role, ctx.OrganizationId, ctx.UserId);
        return user == null ? BadRequest() : Ok(user);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        var user = await _admin.UpdateUserAsync(id, request, ctx.Role, ctx.OrganizationId, ctx.UserId);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost("plants")]
    public async Task<IActionResult> CreatePlant([FromBody] CreatePlantRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        var plant = await _admin.CreatePlantAsync(request, ctx.Role, ctx.OrganizationId);
        return plant == null ? BadRequest() : Ok(plant);
    }

    [HttpGet("plants")]
    public async Task<IActionResult> GetPlants()
    {
        var ctx = UserContext.FromClaims(User);
        var plants = await _access.GetAccessibleSitesAsync(ctx.UserId, ctx.Role, ctx.OrganizationId);
        return Ok(plants);
    }
}
