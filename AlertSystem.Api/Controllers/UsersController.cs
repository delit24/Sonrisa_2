using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertSystem.Api.Models.DTOs.Users;
using AlertSystem.Api.Services;

namespace AlertSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found", code = "NOT_FOUND" });
        }
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var (user, error) = await _userService.CreateAsync(request);

        if (error != null)
        {
            return Conflict(new { error, code = "DUPLICATE_EMAIL" });
        }

        return CreatedAtAction(nameof(GetById), new { id = user!.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var (user, error) = await _userService.UpdateAsync(id, request);

        if (error == "User not found")
        {
            return NotFound(new { error, code = "NOT_FOUND" });
        }

        if (error != null)
        {
            return Conflict(new { error, code = "DUPLICATE_EMAIL" });
        }

        return Ok(user);
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _userService.DeactivateAsync(id);
        if (!result)
        {
            return NotFound(new { error = "User not found", code = "NOT_FOUND" });
        }
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _userService.ActivateAsync(id);
        if (!result)
        {
            return NotFound(new { error = "User not found", code = "NOT_FOUND" });
        }
        return NoContent();
    }
}
