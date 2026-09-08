using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly BillingService _billing;

    public BillingController(BillingService billing) => _billing = billing;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var ctx = UserContext.FromClaims(User);
        var data = await _billing.GetBillingSummaryAsync(ctx.UserId, ctx.Role, ctx.OrganizationId);
        return data == null ? Forbid() : Ok(data);
    }
}
