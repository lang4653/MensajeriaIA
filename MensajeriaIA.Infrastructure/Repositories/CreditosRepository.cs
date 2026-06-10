using Cassandra;
using MensajeriaIA.Core.Models;
using MensajeriaIA.Infrastructure.Data;
using StackExchange.Redis;

namespace MensajeriaIA.Infrastructure.Repositories;

public class CreditosRepository
{
    private readonly ISession _session;
    private readonly IDatabase _redisDb;

    public CreditosRepository(CassandraSessionFactory cassandraFactory, IConnectionMultiplexer redis)
    {
        _session = cassandraFactory.GetSession();
        _redisDb = redis.GetDatabase();
    }

    public async Task<int> CargarSaldoAsync(Guid idUsuario, CargaSaldoRequest request)
    {
        var idOrden = Guid.NewGuid();
        var idTransaccion = Guid.NewGuid();
        var fechaHora = DateTime.UtcNow;

        int creditosComprados = (int)(request.MontoDinero * 10);

        var statementOrden = new SimpleStatement(
            "INSERT INTO ordenes_compra_por_usuario (id_usuario, fecha_hora, id_orden, monto_dinero, moneda, metodo_pago, estado_pago) VALUES (?, ?, ?, ?, ?, ?, ?)",
            idUsuario, fechaHora, idOrden, request.MontoDinero, request.Moneda, "TARJETA", "APROBADO");
        await _session.ExecuteAsync(statementOrden);

        var statementTransaccion = new SimpleStatement(
            "INSERT INTO transacciones_credito_por_usuario (id_usuario, fecha_hora, id_transaccion, monto_creditos, tipo_transaccion, referencia_id) VALUES (?, ?, ?, ?, ?, ?)",
            idUsuario, fechaHora, idTransaccion, creditosComprados, "CARGA_PAYMENT", idOrden);
        await _session.ExecuteAsync(statementTransaccion);

        return (int)await _redisDb.StringIncrementAsync($"usuario:{idUsuario}:saldo", creditosComprados);
    }

    public async Task<(List<TransaccionCredito> Transacciones, byte[]? NextPagingState)> ObtenerTransaccionesPaginadasAsync(Guid idUsuario, int pageSize, byte[]? pagingState)
    {
        var statement = new SimpleStatement("SELECT * FROM transacciones_credito_por_usuario WHERE id_usuario = ?", idUsuario)
            .SetPageSize(pageSize);

        if (pagingState != null)
        {
            statement.SetPagingState(pagingState);
        }

        var rowSet = await _session.ExecuteAsync(statement);

        var transacciones = rowSet.Select(row => new TransaccionCredito
        {
            IdUsuario = row.GetValue<Guid>("id_usuario"),
            FechaHora = row.GetValue<DateTime>("fecha_hora"),
            IdTransaccion = row.GetValue<Guid>("id_transaccion"),
            MontoCreditos = row.GetValue<int>("monto_creditos"),
            TipoTransaccion = row.GetValue<string>("tipo_transaccion"),
            ReferenciaId = row.GetValue<Guid>("referencia_id")
        }).ToList();

        return (transacciones, rowSet.PagingState);
    }
}
