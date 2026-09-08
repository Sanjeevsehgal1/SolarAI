using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScadaController : ControllerBase
{
    private readonly ScadaService _scada;

    public ScadaController(ScadaService scada) => _scada = scada;

    [HttpGet("{siteId}")]
    public async Task<IActionResult> GetSnapshot(int siteId)
    {
        var ctx = UserContext.FromClaims(User);
        var snapshot = await _scada.GetSnapshotAsync(siteId, ctx.UserId, ctx.Role, ctx.OrganizationId);
        return snapshot == null ? NotFound() : Ok(snapshot);
    }
}
