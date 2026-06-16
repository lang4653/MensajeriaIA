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

        // REGLA DE MICROSERVICIOS: Comunicación HTTP hacia el BillingService
        var authHeader = Request.Headers["Authorization"].ToString();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", authHeader); // Le pasamos el token del usuario

        var costoLlamada = 5;
        // El puerto 5002 es el que le asignamos a BillingService
        var response = await client.PostAsJsonAsync("http://localhost:5002/pagos/consumir", new { CostoCreditos = costoLlamada });
        
        if (!response.IsSuccessStatusCode)
            return BadRequest(new { Mensaje = "Saldo insuficiente. Por favor, recarga créditos." });

        await _repo.ActualizarTimestampChatRedisAsync(userId, id);
        _ = ProcesarRespuestaIAAsync(id, request.Contenido);

        return Accepted(new { Mensaje = "Procesando respuesta en SignalR..." });
    }

    private async Task ProcesarRespuestaIAAsync(Guid idConversacion, string prompt)
    {
        var sb = new StringBuilder();
        int tokensIA = 0;
        await foreach (var fragmento in _iaService.GenerarRespuestaStreamAsync(prompt))
        {
            sb.Append(fragmento); tokensIA++;
            await _hubContext.Clients.Group(idConversacion.ToString()).SendAsync("RecibirFragmento", fragmento);
        }
        await _repo.GuardarMensajeAsync(new Mensaje {
            IdConversacion = idConversacion, FechaHora = DateTime.UtcNow, IdMensaje = Guid.NewGuid(),
            RolActor = "assistant", Contenido = sb.ToString(), TokensConsumidos = tokensIA
        });
    }
}
