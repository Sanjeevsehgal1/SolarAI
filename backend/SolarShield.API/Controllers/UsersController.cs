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
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public UsersController(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var ctx = UserContext.FromClaims(User);
        var user = await _db.Users
            .Include(u => u.Organization)
            .Include(u => u.SiteAssignments).ThenInclude(a => a.Site).ThenInclude(s => s.Organization)
            .FirstOrDefaultAsync(u => u.Id == ctx.UserId);

        if (user == null) return NotFound();

        var sites = await _access.GetAccessibleSitesAsync(ctx.UserId, ctx.Role, ctx.OrganizationId);
        var permissions = _access.GetPermissions(ctx.Role);

        return Ok(new UserProfileDto(user.Id, user.FullName, user.Email, user.Phone,
            user.Role.ToString(), user.PreferredLanguage, user.OrganizationId, user.Organization?.Name,
            sites, permissions));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var ctx = UserContext.FromClaims(User);
        var notifications = await _db.Notifications
            .Where(n => n.UserId == ctx.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, n.CreatedAt, n.LinkType, n.LinkId))
            .ToListAsync();
        return Ok(notifications);
    }

    [HttpPut("notifications/{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var ctx = UserContext.FromClaims(User);
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == ctx.UserId);
        if (notification == null) return NotFound();
        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
