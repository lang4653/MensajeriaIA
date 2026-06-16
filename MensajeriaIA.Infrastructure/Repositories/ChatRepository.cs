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
}
