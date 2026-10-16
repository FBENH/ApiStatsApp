using System.Threading.Channels;
using apiBask.Models;
using apiBask.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    // Carga en segundo plano las jugadas de un partido simulado, de a una y con una pausa entre cada una,
    // como si alguien estuviera anotando. Cada jugada sale por SignalR igual que una anotada a mano.
    public class ReproductorDePartidos(IServiceScopeFactory scopes, ILogger<ReproductorDePartidos> log) : BackgroundService
    {
        public record Simulacion(int PartidoId, List<JugadaDto> Jugadas, TimeSpan Pausa);

        private readonly Channel<Simulacion> _cola = Channel.CreateUnbounded<Simulacion>();

        public void Encolar(Simulacion simulacion) => _cola.Writer.TryWrite(simulacion);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Cada partido se reproduce por su cuenta, así puede haber varios en vivo a la vez.
            await foreach (var simulacion in _cola.Reader.ReadAllAsync(stoppingToken))
                _ = Task.Run(() => ReproducirAsync(simulacion, stoppingToken), stoppingToken);
        }

        private async Task ReproducirAsync(Simulacion simulacion, CancellationToken cancelar)
        {
            try
            {
                foreach (var jugada in simulacion.Jugadas)
                {
                    if (simulacion.Pausa > TimeSpan.Zero) await Task.Delay(simulacion.Pausa, cancelar);
                    await using var scope = scopes.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<BasketContext>();
                    var partido = await db.Partidos.FirstOrDefaultAsync(p => p.Id == simulacion.PartidoId, cancelar);
                    // Si alguien descartó o terminó el partido, la simulación se corta.
                    if (partido is not { Activo: true, Estado: EstadoPartido.EnCurso }) return;
                    await scope.ServiceProvider.GetRequiredService<RegistroDePartido>().AgregarAsync(partido, [jugada]);
                }

                await using var final = scopes.CreateAsyncScope();
                var contexto = final.ServiceProvider.GetRequiredService<BasketContext>();
                var terminado = await contexto.Partidos.FirstAsync(p => p.Id == simulacion.PartidoId, cancelar);
                if (terminado.Estado != EstadoPartido.EnCurso) return;
                await final.ServiceProvider.GetRequiredService<RegistroDePartido>()
                    .FinalizarAsync(terminado, (simulacion.Jugadas[^1].Cuarto, 0));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                log.LogError(e, "Falló la simulación del partido {PartidoId}.", simulacion.PartidoId);
            }
        }
    }
}
