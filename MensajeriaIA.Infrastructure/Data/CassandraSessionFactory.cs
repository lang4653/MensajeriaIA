using Cassandra;
using Microsoft.Extensions.Configuration;

namespace MensajeriaIA.Infrastructure.Data;

public class CassandraSessionFactory
{
    private readonly ISession _session;

    public CassandraSessionFactory(IConfiguration configuration)
    {
        var contactPoint = configuration.GetConnectionString("Cassandra") ?? "127.0.0.1";
        // Lee el Keyspace dinámicamente desde el appsettings del microservicio
        var keyspace = configuration["CassandraKeyspace"] ?? "default_keyspace"; 
        
        var cluster = Cluster.Builder()
                             .AddContactPoint(contactPoint)
                             .Build();

        var tempSession = cluster.Connect();
        tempSession.Execute($"CREATE KEYSPACE IF NOT EXISTS {keyspace} WITH replication = {{'class': 'SimpleStrategy', 'replication_factor': '1'}};");
        
        _session = cluster.Connect(keyspace);

        InitializeTables(keyspace);
    }

    public ISession GetSession() => _session;

    private void InitializeTables(string keyspace)
    {
        // El switch asegura el patrón de "Database per Service"
        switch (keyspace)
        {
            case "auth_users":
                _session.Execute("CREATE TABLE IF NOT EXISTS usuarios (id_usuario uuid, email text, password_hash text, nivel_suscripcion text, estado_activo boolean, fecha_registro timestamp, PRIMARY KEY (id_usuario));");
                _session.Execute("CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios (email);");
                break;

            case "billing_credits":
                _session.Execute("CREATE TABLE IF NOT EXISTS transacciones_credito_por_usuario (id_usuario uuid, fecha_hora timestamp, id_transaccion uuid, monto_creditos int, tipo_transaccion text, referencia_id uuid, PRIMARY KEY (id_usuario, fecha_hora, id_transaccion)) WITH CLUSTERING ORDER BY (fecha_hora DESC);");
                _session.Execute("CREATE TABLE IF NOT EXISTS ordenes_compra_por_usuario (id_usuario uuid, fecha_hora timestamp, id_orden uuid, monto_dinero decimal, moneda text, metodo_pago text, estado_pago text, PRIMARY KEY (id_usuario, fecha_hora, id_orden)) WITH CLUSTERING ORDER BY (fecha_hora DESC);");
                _session.Execute("CREATE TABLE IF NOT EXISTS saldo_snapshot_por_usuario (id_usuario uuid, fecha_snapshot timestamp, saldo_acumulado int, PRIMARY KEY (id_usuario, fecha_snapshot)) WITH CLUSTERING ORDER BY (fecha_snapshot DESC);");
                break;

            case "chat_messages":
                _session.Execute("CREATE TABLE IF NOT EXISTS conversaciones_detalle (id_usuario uuid, id_conversacion uuid, titulo_chat text, estado_activo boolean, PRIMARY KEY (id_usuario, id_conversacion));");
                _session.Execute("CREATE TABLE IF NOT EXISTS mensajes_por_conversacion (id_conversacion uuid, fecha_hora timestamp, id_mensaje uuid, rol_actor text, contenido text, tokens_consumidos int, id_modelo_ia text, nombre_modelo_ia text, url_archivo_multimedia text, mime_type_archivo text, PRIMARY KEY (id_conversacion, fecha_hora, id_mensaje)) WITH CLUSTERING ORDER BY (fecha_hora DESC);");
                break;
        }
    }
}