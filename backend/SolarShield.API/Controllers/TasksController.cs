using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarShield.API.Extensions;
using SolarShield.API.Services;
using SolarShield.Core.DTOs;

namespace SolarShield.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskService _tasks;

    public TasksController(TaskService tasks) => _tasks = tasks;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? siteId, [FromQuery] int? userId, [FromQuery] string? status)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _tasks.GetTasksAsync(siteId, userId, status, ctx.UserId, ctx.Role, ctx.OrganizationId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ctx = UserContext.FromClaims(User);
        var task = await _tasks.GetTaskByIdAsync(id, ctx.UserId, ctx.Role, ctx.OrganizationId);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var ctx = UserContext.FromClaims(User);
        return Ok(await _tasks.CreateTaskAsync(request, ctx.UserId));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _tasks.UpdateTaskAsync(id, request);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpPost("from-fault/{faultId}")]
    public async Task<IActionResult> CreateFromFault(int faultId, [FromBody] CreateTaskFromFaultRequest? request)
    {
        var ctx = UserContext.FromClaims(User);
        var task = await _tasks.CreateFromFaultAsync(faultId, request?.AssignedToUserId, ctx.UserId);
        return task == null ? NotFound() : Ok(task);
    }
}
