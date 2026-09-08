using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly AssetService _assets;

    public AssetsController(AssetService assets) => _assets = assets;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? siteId)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _assets.GetAssetsAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ctx = UserContext.FromClaims(User);
        var asset = await _assets.GetAssetByIdAsync(id, ctx.UserId, ctx.Role, ctx.OrganizationId);
        return asset == null ? NotFound() : Ok(asset);
    }
}
