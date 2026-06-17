using Cassandra;
using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Data;
using StackExchange.Redis;

namespace MensajeriaIA.Infrastructure.Repositories;

public class ChatRepository
{
    private readonly ISession _session;
    private readonly IDatabase _redisDb;

    public ChatRepository(CassandraSessionFactory cassandraFactory, IConnectionMultiplexer redis)
    {
        _session = cassandraFactory.GetSession();
        _redisDb = redis.GetDatabase();
    }

    public async Task<Guid> CrearConversacionAsync(Guid idUsuario, string titulo)
    {
        var idConversacion = Guid.NewGuid();
        var statement = new SimpleStatement("INSERT INTO conversaciones_detalle (id_usuario, id_conversacion, titulo_chat, estado_activo) VALUES (?, ?, ?, ?)", idUsuario, idConversacion, titulo, true);
        await _session.ExecuteAsync(statement);

        double score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.SortedSetAddAsync($"usuario:{idUsuario}:chats", idConversacion.ToString(), score);
        return idConversacion;
    }

    public async Task GuardarMensajeAsync(Mensaje mensaje)
    {
        var statement = new SimpleStatement("INSERT INTO mensajes_por_conversacion (id_conversacion, fecha_hora, id_mensaje, rol_actor, contenido, tokens_consumidos, url_archivo_multimedia) VALUES (?, ?, ?, ?, ?, ?, ?)", mensaje.IdConversacion, mensaje.FechaHora, mensaje.IdMensaje, mensaje.RolActor, mensaje.Contenido, mensaje.TokensConsumidos, mensaje.UrlArchivoMultimedia);
        await _session.ExecuteAsync(statement);
    }

    public async Task ActualizarTimestampChatRedisAsync(Guid idUsuario, Guid idConversacion)
    {
        double score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.SortedSetAddAsync($"usuario:{idUsuario}:chats", idConversacion.ToString(), score);
    }

    public async Task<List<ConversacionDetalle>> ObtenerConversacionesRecientesAsync(Guid idUsuario)
    {
        var chatsRedis = await _redisDb.SortedSetRangeByRankAsync($"usuario:{idUsuario}:chats", 0, -1, Order.Descending);
        if (chatsRedis.Length == 0)
            return new List<ConversacionDetalle>();

        var conversaciones = new List<ConversacionDetalle>();
        foreach (var idStr in chatsRedis)
        {
            if (string.IsNullOrWhiteSpace(idStr))
                continue;

            var id = Guid.Parse(idStr.ToString()!);
            var row = (await _session.ExecuteAsync(new SimpleStatement(
                "SELECT * FROM conversaciones_detalle WHERE id_usuario = ? AND id_conversacion = ?",
                idUsuario,
                id
            ))).FirstOrDefault();

            if (row != null && row.GetValue<bool>("estado_activo"))
            {
                conversaciones.Add(new ConversacionDetalle
                {
                    IdUsuario = idUsuario,
                    IdConversacion = id,
                    TituloChat = row.GetValue<string>("titulo_chat"),
                    EstadoActivo = true
                });
            }
        }

        return conversaciones;
    }

    public async Task<List<Mensaje>> ObtenerMensajesAsync(Guid idConversacion)
    {
        var rowSet = await _session.ExecuteAsync(new SimpleStatement(
            "SELECT * FROM mensajes_por_conversacion WHERE id_conversacion = ?",
            idConversacion
        ));

        var mensajes = rowSet.Select(row => new Mensaje
        {
            IdConversacion = idConversacion,
            FechaHora = row.GetValue<DateTime>("fecha_hora"),
            IdMensaje = row.GetValue<Guid>("id_mensaje"),
            RolActor = row.GetValue<string>("rol_actor"),
            Contenido = row.GetValue<string>("contenido"),
            TokensConsumidos = row.GetValue<int>("tokens_consumidos"),
            UrlArchivoMultimedia = row.GetValue<string?>("url_archivo_multimedia")
        }).ToList();

        mensajes.Reverse();
        return mensajes;
    }
}
