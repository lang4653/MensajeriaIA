namespace MensajeriaIA.Core.Models;

public class Usuario
{
    public Guid IdUsuario { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NivelSuscripcion { get; set; } = "GRATUITO";
    public bool EstadoActivo { get; set; } = true;
    public DateTime FechaRegistro { get; set; }
}

public record RegistroRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Mensaje);

// --- DTOs para Pagos y Transacciones ---
public record CargaSaldoRequest(string NumeroTarjeta, decimal MontoDinero, string Moneda);

public class TransaccionCredito
{
    public Guid IdUsuario { get; set; }
    public DateTime FechaHora { get; set; }
    public Guid IdTransaccion { get; set; }
    public int MontoCreditos { get; set; }
    public string TipoTransaccion { get; set; } = string.Empty;
    public Guid ReferenciaId { get; set; }
}