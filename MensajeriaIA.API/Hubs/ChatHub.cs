using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MensajeriaIA.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task UnirseAChat(string idConversacion)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, idConversacion);
    }
}
