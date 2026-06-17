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
        var chats = await _repo.ObtenerConversacionesRecientesAsync(userId);
        return Ok(chats);
    }

    [HttpGet("{id}/mensajes")]
    public async Task<IActionResult> ObtenerHistorial(Guid id)
    {
        var mensajes = await _repo.ObtenerMensajesAsync(id);
        return Ok(mensajes);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConversacion([FromBody] NuevaConversacionRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var idConversacion = await _repo.CrearConversacionAsync(userId, request.Titulo);
        return Ok(new { IdConversacion = idConversacion });
    }

    [HttpPost("{id}/mensajes")]
    public async Task<IActionResult> EnviarMensaje(Guid id, [FromBody] NuevoMensajeRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        await _repo.GuardarMensajeAsync(new Mensaje {
            IdConversacion = id, FechaHora = DateTime.UtcNow, IdMensaje = Guid.NewGuid(),
            RolActor = "user", Contenido = request.Contenido, TokensConsumidos = 0
        });

        var authHeader = Request.Headers["Authorization"].ToString();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", authHeader);

        var costoLlamada = 5;
        var response = await client.PostAsJsonAsync("http://localhost:5002/pagos/consumir", new { CostoCreditos = costoLlamada });
        
        if (!response.IsSuccessStatusCode)
            return BadRequest(new { Mensaje = "Saldo insuficiente. Por favor, recarga créditos." });

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        int nuevoSaldo = result.GetProperty("saldoRestante").GetInt32();

        await _repo.ActualizarTimestampChatRedisAsync(userId, id);
        _ = ProcesarRespuestaIAAsync(id, request.Contenido, request.ModeloId);

        return Accepted(new { Mensaje = "Procesando respuesta...", SaldoRestante = nuevoSaldo });
    }

    private async Task ProcesarRespuestaIAAsync(Guid idConversacion, string prompt, string modeloId)
    {
        var sb = new StringBuilder();
        int tokensIA = 0;
        await foreach (var fragmento in _iaService.GenerarRespuestaStreamAsync(prompt, modeloId))
        {
            sb.Append(fragmento); tokensIA++;
            await _hubContext.Clients.Group(idConversacion.ToString()).SendAsync("RecibirFragmento", fragmento);
        }
        
        string textoFinal = sb.ToString();
        
        // ¡LA SOLUCIÓN! Le avisamos explícitamente al frontend que terminamos de enviar datos
        await _hubContext.Clients.Group(idConversacion.ToString()).SendAsync("FinStream", textoFinal);

        await _repo.GuardarMensajeAsync(new Mensaje {
            IdConversacion = idConversacion, FechaHora = DateTime.UtcNow, IdMensaje = Guid.NewGuid(),
            RolActor = "assistant", Contenido = textoFinal, TokensConsumidos = tokensIA
        });
    }
}
