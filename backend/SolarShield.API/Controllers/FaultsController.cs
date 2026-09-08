using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;
using SolarShield.Core.DTOs;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FaultsController : ControllerBase
{
    private readonly FaultService _faults;

    public FaultsController(FaultService faults) => _faults = faults;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? siteId, [FromQuery] string? status, [FromQuery] string? severity)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _faults.GetFaultsAsync(siteId, status, severity, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ctx = UserContext.FromClaims(User);
        var fault = await _faults.GetFaultByIdAsync(id, ctx.UserId, ctx.Role, ctx.OrganizationId);
        return fault == null ? NotFound() : Ok(fault);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFaultRequest request)
        => Ok(await _faults.CreateFaultAsync(request));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFaultRequest request)
    {
        var fault = await _faults.UpdateFaultAsync(id, request);
        return fault == null ? NotFound() : Ok(fault);
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        var ok = await _faults.AddCommentAsync(id, ctx.UserId, request.Comment);
        return ok ? Ok() : NotFound();
    }

    [HttpPost("predict/{siteId}")]
    public async Task<IActionResult> RunPrediction(int siteId)
        => Ok(await _faults.RunPredictionAsync(siteId));

    [HttpPost("{id}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var fault = await _faults.UpdateFaultAsync(id, new UpdateFaultRequest(Status: "Acknowledged", AssignedToUserId: null, FixNotes: null, PhotoUrl: null));
        return fault == null ? NotFound() : Ok(fault);
    }

    [HttpPost("{id}/escalate")]
    public async Task<IActionResult> Escalate(int id)
    {
        var fault = await _faults.UpdateFaultAsync(id, new UpdateFaultRequest(Status: "Escalated", AssignedToUserId: null, FixNotes: null, PhotoUrl: null));
        return fault == null ? NotFound() : Ok(fault);
    }
}
