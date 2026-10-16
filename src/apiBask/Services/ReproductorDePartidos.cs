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

        // Pone en curso un partido programado y encola su simulación con los jugadores activos de los dos equipos.
        // Devuelve cuántas jugadas tiene, o null si a algún equipo le faltan jugadores.
        public async Task<int?> EmpezarAsync(BasketContext db, AvisosEnVivo avisos, Partido partido, TimeSpan pausa)
        {
            var jugadores = await db.Jugadores.AsNoTracking()
                .Where(j => (j.EquipoId == partido.LocalId || j.EquipoId == partido.VisitanteId) && j.Activo)
                .OrderBy(j => j.Numero)
                .Select(j => new { j.Id, j.EquipoId })
                .ToListAsync();
            var locales = jugadores.Where(j => j.EquipoId == partido.LocalId).Select(j => j.Id).ToList();
            var visitantes = jugadores.Where(j => j.EquipoId == partido.VisitanteId).Select(j => j.Id).ToList();
            if (locales.Count == 0 || visitantes.Count == 0) return null;

            partido.Estado = EstadoPartido.EnCurso;
            partido.Fecha ??= DateTime.UtcNow.Date;
            await db.SaveChangesAsync();
            await avisos.PartidoAsync(partido);
            var jugadas = Simulador.Generar(partido, locales, visitantes, Random.Shared);
            Encolar(new(partido.Id, jugadas, pausa));
            return jugadas.Count;
        }

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
                var terminado = await contexto.Partidos.FirstOrDefaultAsync(p => p.Id == simulacion.PartidoId, cancelar);
                if (terminado is not { Estado: EstadoPartido.EnCurso }) return;
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
