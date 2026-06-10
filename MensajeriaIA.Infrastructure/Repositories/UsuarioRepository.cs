using Cassandra;
using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Data;
using StackExchange.Redis;

namespace MensajeriaIA.Infrastructure.Repositories;

public class UsuarioRepository
{
    private readonly ISession _session;
    private readonly IDatabase _redisDb;

    public UsuarioRepository(CassandraSessionFactory cassandraFactory, IConnectionMultiplexer redis)
    {
        _session = cassandraFactory.GetSession();
        _redisDb = redis.GetDatabase();
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        var statement = new SimpleStatement("SELECT * FROM usuarios WHERE email = ?", email);
        var row = (await _session.ExecuteAsync(statement)).FirstOrDefault();

        if (row == null) return null;

        return new Usuario
        {
            IdUsuario = row.GetValue<Guid>("id_usuario"),
            Email = row.GetValue<string>("email"),
            PasswordHash = row.GetValue<string>("password_hash"),
            NivelSuscripcion = row.GetValue<string>("nivel_suscripcion"),
            EstadoActivo = row.GetValue<bool>("estado_activo"),
            FechaRegistro = row.GetValue<DateTime>("fecha_registro")
        };
    }

    public async Task<Guid> RegistrarUsuarioAsync(RegistroRequest request)
    {
        var nuevoId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var fechaRegistro = DateTime.UtcNow;

        // 1. Guardar en Cassandra
        var statement = new SimpleStatement(
            "INSERT INTO usuarios (id_usuario, email, password_hash, nivel_suscripcion, estado_activo, fecha_registro) VALUES (?, ?, ?, ?, ?, ?)",
            nuevoId, request.Email, passwordHash, "GRATUITO", true, fechaRegistro);
        
        await _session.ExecuteAsync(statement);

        // 2. Establecer saldo inicial (100 créditos) en Redis atómicamente
        await _redisDb.StringSetAsync($"usuario:{nuevoId}:saldo", 100);

        return nuevoId;
    }

    public bool ValidarPassword(string passwordPlana, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(passwordPlana, passwordHash);
    }
}