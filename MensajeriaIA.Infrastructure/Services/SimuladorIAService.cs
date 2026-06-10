namespace MensajeriaIA.Infrastructure.Services;

public class SimuladorIAService
{
    public async IAsyncEnumerable<string> GenerarRespuestaStreamAsync(string prompt)
    {
        var palabras = $"He analizado tu mensaje: '{prompt}'. Esta es una respuesta simulada por la plataforma de Mensajería IA, transmitida token a token mediante SignalR para garantizar la mejor experiencia en tiempo real.".Split(' ');

        foreach (var palabra in palabras)
        {
            await Task.Delay(100);
            yield return palabra + " ";
        }
    }
}
