using MensajeriaIA.Infrastructure.Data;
using MensajeriaIA.Infrastructure.Repositories;
using MensajeriaIA.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configurar Redis ---
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// --- 2. Configurar Cassandra y Repositorios ---
builder.Services.AddSingleton<CassandraSessionFactory>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<CreditosRepository>();
builder.Services.AddScoped<ChatRepository>();
builder.Services.AddSingleton<SimuladorIAService>();
builder.Services.AddSignalR();

// --- 3. Configurar JWT ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// --- 4. Forzar la inicialización de Cassandra al arrancar ---
// Al pedir el servicio, el constructor de la fábrica se ejecuta y crea el Keyspace/Tablas
using (var scope = app.Services.CreateScope())
{
    var cassandraFactory = scope.ServiceProvider.GetRequiredService<CassandraSessionFactory>();
}

// --- 5. Endpoint de Health Check ---
app.MapGet("/health", ([FromServices] CassandraSessionFactory cassandraFactory, [FromServices] IConnectionMultiplexer redis) =>
{
    try
    {
        // Probar Cassandra (obtenemos la versión del cluster local)
        var cassandraSession = cassandraFactory.GetSession();
        var cassandraRow = cassandraSession.Execute("SELECT release_version FROM system.local").FirstOrDefault();
        string cassandraStatus = cassandraRow != null ? $"Connected (v{cassandraRow.GetValue<string>("release_version")})" : "Unknown";

        // Probar Redis (hacemos un Ping)
        var redisDb = redis.GetDatabase();
        var redisPing = redisDb.Ping();

        return Results.Ok(new
        {
            Status = "Todo operativo 😎",
            Cassandra = cassandraStatus,
            RedisPing = $"{redisPing.TotalMilliseconds} ms"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Error conectando a las bases de datos");
    }
});

app.MapControllers();
app.MapHub<MensajeriaIA.API.Hubs.ChatHub>("/chat");

app.Run();