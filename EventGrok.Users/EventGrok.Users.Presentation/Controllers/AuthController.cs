using EventGrok.Users.Application.DTOs;
using EventGrok.Users.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventGrok.Users.Presentation.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserInfoDto>> Register(RegisterDto dto, CancellationToken ct = default)
    {
        UserInfoDto user = await userService.RegisterAsync(dto, ct);
        
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginDto dto, CancellationToken ct = default)
    {
        TokenResponseDto token = await userService.LoginAsync(dto, ct);
        
        return token;
    }
}