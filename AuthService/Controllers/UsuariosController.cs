using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IDatabase _redisDb;

    public UsuariosController(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var saldo = await _redisDb.StringGetAsync($"usuario:{userId}:saldo");

        return Ok(new
        {
            Id = userId,
            Email = email,
            Saldo = saldo.HasValue ? (int)saldo : 0
        });
    }
}
