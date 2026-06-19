using ChatService.Hubs;
using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Repositories;
using MensajeriaIA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ChatService.Controllers;

[ApiController]
[Route("conversaciones")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatRepository _repo;
    private readonly SimuladorIAService _iaService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatController(ChatRepository repo, SimuladorIAService iaService, IHubContext<ChatHub> hubContext, IHttpClientFactory httpClientFactory)
    {
        _repo = repo; _iaService = iaService; _hubContext = hubContext; _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerConversaciones()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _repo.ObtenerConversacionesRecientesAsync(userId));
    }

    [HttpGet("{id}/mensajes")]
    public async Task<IActionResult> ObtenerHistorial(Guid id)
    {
        return Ok(await _repo.ObtenerMensajesAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> CrearConversacion([FromBody] NuevaConversacionRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return Ok(new { IdConversacion = await _repo.CrearConversacionAsync(userId, request.Titulo) });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarConversacion(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _repo.EliminarConversacionAsync(userId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Si Cassandra falla, ahora lo veremos en rojo en tu navegador
            return StatusCode(500, new { Mensaje = ex.Message });
        }
    }

    // --- NUEVO ENDPOINT: Renombrar Chat ---
    [HttpPut("{id}")]
    public async Task<IActionResult> RenombrarConversacion(Guid id, [FromBody] RenombrarRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _repo.RenombrarConversacionAsync(userId, id, request.Titulo);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Mensaje = ex.Message });
        }
    }

    [HttpPost("{id}/mensajes")]
    public async Task<IActionResult> EnviarMensaje(Guid id, [FromBody] NuevoMensajeRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        string authHeader = Request.Headers["Authorization"].ToString();

        await _repo.GuardarMensajeAsync(new Mensaje {
            IdConversacion = id, FechaHora = DateTime.UtcNow, IdMensaje = Guid.NewGuid(),
            RolActor = "user", Contenido = request.Contenido, TokensConsumidos = 0
        });

        await _repo.ActualizarTimestampChatRedisAsync(userId, id);
        _ = ProcesarRespuestaIAAsync(id, request.Contenido, request.ModeloId, authHeader);

        return Accepted(new { Mensaje = "Generando respuesta..." });
    }

    private async Task ProcesarRespuestaIAAsync(Guid idConversacion, string prompt, string modeloId, string jwtToken)
    {
        var sb = new StringBuilder();
        
        await foreach (var fragmento in _iaService.GenerarRespuestaStreamAsync(prompt, modeloId))
        {
            sb.Append(fragmento);
            await _hubContext.Clients.Group(idConversacion.ToString()).SendAsync("RecibirFragmento", fragmento);
        }
        
        string textoFinal = sb.ToString();
        await _hubContext.Clients.Group(idConversacion.ToString()).SendAsync("FinStream", textoFinal);

        int tokensEstimados = Math.Max(1, textoFinal.Length / 4);
        int costoCreditos = (int)Math.Ceiling(tokensEstimados * 0.05);
        if (costoCreditos < 1) costoCreditos = 1;

        await _repo.GuardarMensajeAsync(new Mensaje {
            IdConversacion = idConversacion,
            FechaHora = DateTime.UtcNow,
            IdMensaje = Guid.NewGuid(),
            RolActor = "assistant",
            Contenido = textoFinal,
            TokensConsumidos = tokensEstimados
        });

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", jwtToken);

        var response = await client.PostAsJsonAsync("http://localhost:5002/pagos/consumir", new { CostoCreditos = costoCreditos, ModeloId = modeloId });
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            await _hubContext.Clients.Group(idConversacion.ToString()).SendAsync("ActualizarSaldo", result.GetProperty("saldoRestante").GetInt32());
        }
    }
}
