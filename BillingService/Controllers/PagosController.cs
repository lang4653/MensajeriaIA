using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillingService.Controllers;

public record ConsumoRequest(int CostoCreditos);

[ApiController]
[Route("pagos")]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly CreditosRepository _repo;
    public PagosController(CreditosRepository repo) { _repo = repo; }

    [HttpPost("cargar-saldo")]
    public async Task<IActionResult> CargarSaldo([FromBody] CargaSaldoRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var nuevoSaldo = await _repo.CargarSaldoAsync(userId, request);
        return Ok(new { Mensaje = "Saldo recargado exitosamente", NuevoSaldo = nuevoSaldo });
    }

    [HttpGet("transacciones")]
    public async Task<IActionResult> HistorialTransacciones([FromQuery] int pageSize = 10, [FromQuery] string? pagingState = null)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        byte[]? estadoPaginacion = string.IsNullOrEmpty(pagingState) ? null : Convert.FromBase64String(pagingState);
        var resultado = await _repo.ObtenerTransaccionesPaginadasAsync(userId, pageSize, estadoPaginacion);
        return Ok(new { Items = resultado.Transacciones, NextPagingState = resultado.NextPagingState != null ? Convert.ToBase64String(resultado.NextPagingState) : null });
    }

    // NUEVO ENDPOINT PARA USO INTERNO DE LOS MICROSERVICIOS
    [HttpPost("consumir")]
    public async Task<IActionResult> ConsumirCreditos([FromBody] ConsumoRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var saldoRestante = await _repo.DescontarCreditosAsync(userId, request.CostoCreditos);
        
        if (saldoRestante == -1) return BadRequest(new { Mensaje = "Saldo insuficiente" });
        return Ok(new { SaldoRestante = saldoRestante });
    }
}
