namespace MensajeriaIA.Core.Models;

public class ConversacionDetalle
{
    public Guid IdUsuario { get; set; }
    public Guid IdConversacion { get; set; }
    public string TituloChat { get; set; } = string.Empty;
    public bool EstadoActivo { get; set; } = true;
}

public class Mensaje
{
    public Guid IdConversacion { get; set; }
    public DateTime FechaHora { get; set; }
    public Guid IdMensaje { get; set; }
    public string RolActor { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public int TokensConsumidos { get; set; }
    public string? UrlArchivoMultimedia { get; set; }
}

public record NuevaConversacionRequest(string Titulo);

// Aquí reemplazamos el antiguo 'record' por la clase completa que recibe la IA
public class NuevoMensajeRequest
{
    public string Contenido { get; set; } = string.Empty;
    public string ModeloId { get; set; } = "gemini";
}
