using Cassandra;
using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Data;
using StackExchange.Redis;

namespace MensajeriaIA.Infrastructure.Repositories;

public class ChatRepository
{
    private readonly ISession _session;
    private readonly IDatabase _redisDb;

    private const string LUA_DEDUCIR_SALDO = @"
        local key = KEYS[1]
        local coste = tonumber(ARGV[1])
        local saldo = tonumber(redis.call('GET', key) or 0)
        if saldo >= coste then
            redis.call('DECRBY', key, coste)
            return saldo - coste
        else
            return -1
        end
    ";

    public ChatRepository(CassandraSessionFactory cassandraFactory, IConnectionMultiplexer redis)
    {
        _session = cassandraFactory.GetSession();
        _redisDb = redis.GetDatabase();
    }

    public async Task<Guid> CrearConversacionAsync(Guid idUsuario, string titulo)
    {
        var idConversacion = Guid.NewGuid();

        var statement = new SimpleStatement(
            "INSERT INTO conversaciones_detalle (id_usuario, id_conversacion, titulo_chat, estado_activo) VALUES (?, ?, ?, ?)",
            idUsuario, idConversacion, titulo, true);
        await _session.ExecuteAsync(statement);

        double score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.SortedSetAddAsync($"usuario:{idUsuario}:chats", idConversacion.ToString(), score);

        return idConversacion;
    }

    public async Task GuardarMensajeAsync(Mensaje mensaje)
    {
        var statement = new SimpleStatement(
            "INSERT INTO mensajes_por_conversacion (id_conversacion, fecha_hora, id_mensaje, rol_actor, contenido, tokens_consumidos, url_archivo_multimedia) VALUES (?, ?, ?, ?, ?, ?, ?)",
            mensaje.IdConversacion, mensaje.FechaHora, mensaje.IdMensaje, mensaje.RolActor, mensaje.Contenido, mensaje.TokensConsumidos, mensaje.UrlArchivoMultimedia);

        await _session.ExecuteAsync(statement);
    }

    public async Task ActualizarTimestampChatRedisAsync(Guid idUsuario, Guid idConversacion)
    {
        double score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redisDb.SortedSetAddAsync($"usuario:{idUsuario}:chats", idConversacion.ToString(), score);
    }

    public async Task<int> DescontarCreditosLlamadaIAAsync(Guid idUsuario, int costeCreditos)
    {
        var result = await _redisDb.ScriptEvaluateAsync(
            LUA_DEDUCIR_SALDO,
            new RedisKey[] { $"usuario:{idUsuario}:saldo" },
            new RedisValue[] { costeCreditos });

        return (int)result;
    }
}
