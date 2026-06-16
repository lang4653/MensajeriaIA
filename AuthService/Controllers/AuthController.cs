using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UsuarioRepository _repo;
    private readonly IConfiguration _config;

    public AuthController(UsuarioRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
    {
        var existe = await _repo.ObtenerPorEmailAsync(request.Email);
        if (existe != null) return BadRequest("El correo ya está registrado.");

        var idUsuario = await _repo.RegistrarUsuarioAsync(request);
        return Ok(new { Mensaje = "Usuario registrado exitosamente", IdUsuario = idUsuario });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _repo.ObtenerPorEmailAsync(request.Email);
        if (usuario == null || !_repo.ValidarPassword(request.Password, usuario.PasswordHash))
            return Unauthorized("Credenciales inválidas.");

        var token = GenerarJwtToken(usuario);
        return Ok(new AuthResponse(token, "Login exitoso"));
    }

    private string GenerarJwtToken(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
