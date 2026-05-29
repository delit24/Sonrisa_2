using Microsoft.AspNetCore.Mvc;
using AlertSystem.Api.Models.DTOs.Auth;
using AlertSystem.Api.Services;

namespace AlertSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response == null)
        {
            return Unauthorized(new { error = "Invalid credentials", code = "INVALID_CREDENTIALS" });
        }

        return Ok(response);
    }
}
